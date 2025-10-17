use anyhow::Result;
use regex::Regex;
use std::fs;

pub fn generate_version_string(_build_mode: &str) -> Result<String> {
    const ROOT_TOML_PATH: &str = "Cargo.toml";
    let root_toml_content = fs::read_to_string(ROOT_TOML_PATH)?;
    let root_toml: toml::Value = toml::from_str(&root_toml_content)?;

    let base_version = extract_base_version(&root_toml);
    let current_build_number = extract_build_number()?;

    let version_string = format!(
        "{}-{:05}",
        base_version.replace('.', "-"),
        current_build_number + 1
    );

    update_core_cs_version(&version_string)?;
    Ok(version_string)
}

fn extract_base_version(root_toml: &toml::Value) -> &str {
    root_toml
        .get("package")
        .and_then(|p| p.get("version"))
        .and_then(|v| v.as_str())
        .unwrap_or("0.1.0")
}

fn extract_build_number() -> Result<u32> {
    const CORE_CS_PATH: &str = "Assets/Scripts/Core/Core.cs";
    let core_cs_content = match fs::read_to_string(CORE_CS_PATH) {
        Ok(content) => content,
        Err(_) => return Ok(0),
    };

    let num_re = Regex::new(r#"    public const string version = "\d+-\d+-\d+-(\d{5})";"#)?;
    Ok(num_re
        .captures(&core_cs_content)
        .and_then(|caps| caps.get(1))
        .and_then(|m| m.as_str().parse().ok())
        .unwrap_or(0))
}

fn update_core_cs_version(version_string: &str) -> Result<()> {
    const CORE_CS_PATH: &str = "Assets/Scripts/Core/Core.cs";
    let core_cs_content = match fs::read_to_string(CORE_CS_PATH) {
        Ok(content) => content,
        Err(_) => return Ok(()),
    };

    let updated_content = core_cs_content
        .lines()
        .map(|line| {
            if line.contains("public const string version =") {
                format!("    public const string version = \"{version_string}\";")
            } else {
                line.to_string()
            }
        })
        .collect::<Vec<_>>()
        .join("\n");

    let _ = fs::write(CORE_CS_PATH, updated_content);
    Ok(())
}
