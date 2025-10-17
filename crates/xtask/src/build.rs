use anyhow::Result;
use colored::*;
use std::fs;
use std::path::{Path, PathBuf};
use std::process::Command;

use crate::targets::{TargetConfig, get_target_configs};
use crate::version::generate_version_string;

pub fn build_all_targets(release: bool) -> Result<()> {
    const PLUGINS_DIR: &str = "Assets/Plugins";
    let plugins_dir = PathBuf::from(PLUGINS_DIR);

    println!("{}\n", "🔧 Building Core Libraries".bold().cyan());

    let version_string = generate_and_print_version(release)?;
    fs::create_dir_all(&plugins_dir)?;

    build_rust_libraries(release)?;
    copy_all_artifacts(&plugins_dir, &version_string, release)?;

    println!();
    println!("{}", "✅ Build completed successfully!".green().bold());
    Ok(())
}

fn generate_and_print_version(release: bool) -> Result<String> {
    print!("{}", "📋 Generating version... ".dimmed());
    let build_mode = if release { "release" } else { "debug" };
    let version_string = generate_version_string(build_mode)?;
    println!("{}", format!("v{version_string}").green().bold());
    Ok(version_string)
}

fn build_rust_libraries(release: bool) -> Result<()> {
    let mode_color = if release {
        "release".red()
    } else {
        "debug".yellow()
    };

    println!();
    println!(
        "{} {}",
        "🦀 Building Rust libraries in".dimmed(),
        mode_color.bold()
    );

    let targets = get_target_configs();
    for target in &targets {
        build_single_target(target, release)?;
    }

    Ok(())
}

fn build_single_target(target: &TargetConfig, release: bool) -> Result<()> {
    println!();
    println!("Building {}", target.name.bold());

    let mut cmd = Command::new("cargo");
    cmd.args(["rustc", "--package", "core"]);
    cmd.args(&target.extra_args);

    if release {
        cmd.arg("--release");
    }

    if let Some(triple) = &target.target_triple {
        cmd.args(["--target", triple]);
    }
    cmd.args(["--crate-type", target.crate_type]);

    if !target.rustflags.is_empty() {
        let rustflags = target.rustflags.join(" ");
        cmd.env("RUSTFLAGS", rustflags);
    }

    let status = cmd.status()?;
    if !status.success() {
        anyhow::bail!("Failed to build {} target", target.name);
    }

    Ok(())
}

fn copy_all_artifacts(plugins_dir: &Path, version_string: &str, release: bool) -> Result<()> {
    println!();
    println!("{}", "📦 Copying artifacts:".dimmed());

    let build_mode = if release { "release" } else { "debug" };
    let targets = get_target_configs();

    for target in &targets {
        let target_dir = format!(
            "target/{}/{}",
            target.target_triple.unwrap_or_default(),
            build_mode
        );

        copy_target_artifacts(&target_dir, plugins_dir, target, version_string, build_mode)?;
    }

    Ok(())
}

fn copy_target_artifacts(
    build_dir: &str,
    plugins_dir: &Path,
    target: &TargetConfig,
    version: &str,
    build_mode: &str,
) -> Result<()> {
    let build_path = PathBuf::from(build_dir);
    if !build_path.exists() {
        println!(
            "   {} {}",
            "⚠️".yellow(),
            format!("No artifacts found for {}", target.name).dimmed()
        );
        return Ok(());
    }

    let entries = fs::read_dir(&build_path)?;
    for entry in entries {
        let entry = entry?;
        let path = entry.path();

        if let Some(extension) = path.extension() {
            let ext_str = extension.to_string_lossy();
            if matches!(ext_str.as_ref(), "so" | "dylib" | "dll" | "a")
                && let Some(stem) = path.file_stem()
            {
                let stem_str = stem.to_string_lossy();
                if stem_str == "libcore" || stem_str == "core" {
                    copy_single_artifact(
                        &path,
                        plugins_dir,
                        target,
                        version,
                        build_mode,
                        &ext_str,
                    )?;
                }
            }
        }
    }

    Ok(())
}

fn copy_single_artifact(
    source_path: &Path,
    plugins_dir: &Path,
    target: &TargetConfig,
    version: &str,
    build_mode: &str,
    extension: &str,
) -> Result<()> {
    let dest_path = match target.name {
        "webgl" => {
            let webgl_dir = plugins_dir.join("WebGL");
            fs::create_dir_all(&webgl_dir)?;
            webgl_dir.join(format!("core.{extension}"))
        }
        _ => plugins_dir.join(format!(
            "libcore_{}_{}_{}.{}",
            target.name, build_mode, version, extension
        )),
    };

    println!(
        "   {} {}",
        target.name.bold(),
        dest_path.display().to_string().blue()
    );
    fs::copy(source_path, &dest_path)?;

    Ok(())
}
