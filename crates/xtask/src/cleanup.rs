use anyhow::Result;
use colored::*;
use regex::Regex;
use std::fs;
use std::path::{Path, PathBuf};

use crate::version::generate_version_string;

pub fn cleanup_old_libraries() -> Result<()> {
    const PLUGINS_DIR: &str = "Assets/Plugins";
    let plugins_dir = PathBuf::from(PLUGINS_DIR);

    println!("{}", "🧹 Cleaning up old library versions".bold().cyan());

    if !plugins_dir.exists() {
        println!(
            "{}",
            "No plugins directory found, nothing to clean".dimmed()
        );
        return Ok(());
    }

    let current_version = get_current_version_info()?;
    let files_to_remove = find_old_library_files(&plugins_dir, &current_version)?;

    if files_to_remove.is_empty() {
        println!("{}", "No old library files found to remove".green());
        return Ok(());
    }

    let mut deleted_count = 0;
    let mut failed_count = 0;

    for file in files_to_remove {
        match fs::remove_file(&file) {
            Ok(_) => deleted_count += 1,
            Err(_) => failed_count += 1,
        }
    }

    if failed_count > 0 {
        println!(
            "{} {} files deleted, {} failed",
            "✅".green(),
            deleted_count,
            failed_count
        );
    } else {
        println!("{} {} files deleted", "✅".green(), deleted_count);
    }
    Ok(())
}

#[derive(Debug)]
struct VersionInfo {
    build_number: u32,
    date_string: String,
}

fn get_current_version_info() -> Result<VersionInfo> {
    let version_string = generate_version_string("debug")?;
    parse_version_string(&version_string)
}

fn parse_version_string(version: &str) -> Result<VersionInfo> {
    let re = Regex::new(r"(\d+-\d+-\d+)-(\d{5})")?;
    if let Some(captures) = re.captures(version) {
        let date_string = captures.get(1).unwrap().as_str().to_string();
        let build_number = captures.get(2).unwrap().as_str().parse::<u32>()?;
        Ok(VersionInfo {
            build_number,
            date_string,
        })
    } else {
        anyhow::bail!("Invalid version format: {}", version);
    }
}

fn find_old_library_files(
    plugins_dir: &Path,
    current_version: &VersionInfo,
) -> Result<Vec<PathBuf>> {
    let mut files_to_remove = Vec::new();

    scan_directory_for_old_files(plugins_dir, current_version, &mut files_to_remove)?;

    Ok(files_to_remove)
}

fn scan_directory_for_old_files(
    dir: &Path,
    current_version: &VersionInfo,
    files_to_remove: &mut Vec<PathBuf>,
) -> Result<()> {
    if !dir.is_dir() {
        return Ok(());
    }

    for entry in fs::read_dir(dir)? {
        let entry = entry?;
        let path = entry.path();

        if path.is_dir() {
            scan_directory_for_old_files(&path, current_version, files_to_remove)?;
        } else if let Some(filename) = path.file_name().and_then(|n| n.to_str()) {
            if should_remove_file(filename, current_version) {
                files_to_remove.push(path);
            }
        }
    }

    Ok(())
}

fn should_remove_file(filename: &str, current_version: &VersionInfo) -> bool {
    if !is_library_file(filename) {
        return false;
    }

    if let Ok(Some(file_version)) = extract_version_from_filename(filename) {
        // Remove if date is before current date
        if file_version.date_string < current_version.date_string {
            return true;
        }

        // Remove if build number is 10 or more builds behind current
        if current_version.build_number >= 10
            && file_version.build_number <= current_version.build_number - 10
        {
            return true;
        }
    }

    false
}

fn is_library_file(filename: &str) -> bool {
    (filename.starts_with("libcore_") || filename.starts_with("core."))
        && matches!(
            filename.split('.').last(),
            Some("so" | "dylib" | "dll" | "a")
        )
}

fn extract_version_from_filename(filename: &str) -> Result<Option<VersionInfo>> {
    // Match patterns like: libcore_android_debug_0-1-0-00001.so or core.a
    let re = Regex::new(r"(?:libcore_\w+_\w+_|core\.)(\d+-\d+-\d+)-(\d{5})")?;

    if let Some(captures) = re.captures(filename) {
        let date_string = captures.get(1).unwrap().as_str().to_string();
        let build_number = captures.get(2).unwrap().as_str().parse::<u32>()?;
        Ok(Some(VersionInfo {
            build_number,
            date_string,
        }))
    } else {
        Ok(None)
    }
}
