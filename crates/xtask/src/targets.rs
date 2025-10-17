#[derive(Debug, Clone)]
pub struct TargetConfig {
    pub name: &'static str,
    pub target_triple: Option<&'static str>,
    pub crate_type: &'static str,
    pub rustflags: Vec<&'static str>,
    pub extra_args: Vec<&'static str>,
}

impl Default for TargetConfig {
    fn default() -> Self {
        Self {
            name: "native",
            target_triple: None,
            crate_type: "cdylib",
            rustflags: vec![],
            extra_args: vec![],
        }
    }
}

impl TargetConfig {
    pub fn native() -> Self {
        Self::default()
    }

    pub fn webgl() -> Self {
        Self {
            name: "webgl",
            target_triple: Some("wasm32-unknown-unknown"),
            crate_type: "staticlib",
            rustflags: vec!["-C", "target-cpu=mvp"],
            extra_args: vec!["-Z", "build-std=panic_abort,std"],
        }
    }

    pub fn ios() -> Self {
        Self {
            name: "ios",
            target_triple: Some("aarch64-apple-ios"),
            crate_type: "staticlib",
            ..Default::default()
        }
    }

    pub fn macos_universal() -> Self {
        Self {
            name: "macos_universal",
            target_triple: Some("universal-apple-darwin"),
            ..Default::default()
        }
    }
}

pub fn get_target_configs() -> Vec<TargetConfig> {
    let mut configs = vec![TargetConfig::native(), TargetConfig::webgl()];

    if cfg!(target_os = "macos") {
        configs.extend(vec![TargetConfig::ios(), TargetConfig::macos_universal()]);
    }

    configs
}
