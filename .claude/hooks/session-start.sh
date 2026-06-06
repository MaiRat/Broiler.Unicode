#!/bin/bash
# SessionStart hook: provision the .NET 10 SDK so builds, the "linter"
# (TreatWarningsAsErrors build) and tests work in Claude Code on the web.
set -euo pipefail

# Only run in the remote (Claude Code on the web) environment.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Install the .NET 10 SDK if it isn't already present. The dot.net /
# Microsoft binary CDNs are blocked by the network allowlist, so we use
# the Ubuntu package feed (noble-updates / noble-security) instead.
if ! command -v dotnet >/dev/null 2>&1 || ! dotnet --list-sdks 2>/dev/null | grep -q '^10\.'; then
  export DEBIAN_FRONTEND=noninteractive
  # Third-party PPAs may be unsigned/blocked; don't let that abort the run.
  sudo apt-get update || true
  # SDK 10 builds everything; the .NET 8 runtime lets the net8.0 test
  # target (the library multi-targets net8.0;net10.0) run as well.
  sudo apt-get install -y dotnet-sdk-10.0 dotnet-runtime-8.0
fi

# Persist the env vars for the rest of the session.
{
  echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1'
  echo 'export DOTNET_NOLOGO=1'
  echo 'export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1'
} >> "$CLAUDE_ENV_FILE"

# Warm the restore cache so later builds/tests are fast (cached in the
# container image). Restoring the test project pulls in the library and
# generator it references.
dotnet restore "$CLAUDE_PROJECT_DIR/tests/UnicodeEmoji.StringProperties.Tests/UnicodeEmoji.StringProperties.Tests.csproj"
dotnet restore "$CLAUDE_PROJECT_DIR/src/UnicodeEmoji.StringProperties.DataTool/UnicodeEmoji.StringProperties.DataTool.csproj"
dotnet restore "$CLAUDE_PROJECT_DIR/benchmarks/UnicodeEmoji.StringProperties.Benchmarks/UnicodeEmoji.StringProperties.Benchmarks.csproj"
