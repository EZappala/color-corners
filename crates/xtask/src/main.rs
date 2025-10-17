use anyhow::Result;
use clap::{Parser, Subcommand};

mod build;
mod cleanup;
mod targets;
mod version;

use build::build_all_targets;
use cleanup::cleanup_old_libraries;

#[derive(Parser)]
#[command(name = "xtask")]
struct Cli {
    #[command(subcommand)]
    command: Commands,
}

#[derive(Subcommand)]
enum Commands {
    /// Build all target libraries
    Build {
        /// Build in release mode
        #[arg(long)]
        release: bool,
    },
    /// Clean up old library versions
    Cleanup,
}

fn main() -> Result<()> {
    let cli = Cli::parse();

    match cli.command {
        Commands::Build { release } => build_all_targets(release),
        Commands::Cleanup => cleanup_old_libraries(),
    }
}
