# Contributing to ZenDisk

Thanks for your interest in contributing to ZenDisk.

This guide describes a standard GitHub contribution workflow, project coding expectations, and commit message format.

## Everyone Is Welcome

Everybody is welcome to contribute, no matter your experience level.
Please do not be shy about making mistakes - mistakes are part of learning, and review is here to help.

## Prerequisites

- Windows (project targets `net9.0-windows`)
- .NET 9 SDK
- Git

## Getting Started

1. Fork this repository on GitHub.
2. Clone your fork:
   ```bash
   git clone https://github.com/<your-username>/ZenDisk.git
   cd ZenDisk
   ```
3. Create a branch from `main`:
   ```bash
   git checkout -b fix/short-description
   ```
4. Restore and build:
   ```bash
   dotnet restore
   dotnet build
   ```
5. Run the app:
   ```bash
   dotnet run
   ```

## Standard GitHub Flow

1. Sync your branch with latest `main`.
2. Commit using Conventional Commits.
3. Push your branch and open a Pull Request.

## Commit Messages (Conventional Commits)

ZenDisk uses [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/).
