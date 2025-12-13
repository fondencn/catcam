# Branch Protection Configuration

This document describes the recommended branch protection settings for the `main` branch.

## Required Settings

To protect the main branch and enforce the CI pipeline, configure the following settings in your GitHub repository:

### Steps to Configure Branch Protection:

1. Go to your repository on GitHub
2. Navigate to **Settings** → **Branches**
3. Click **Add rule** under "Branch protection rules"
4. Configure the following:

### Branch name pattern:
```
main
```

### Protection Rules:

#### Require a pull request before merging
- ✅ **Require a pull request before merging**
  - Require approvals: 1
  - Dismiss stale pull request approvals when new commits are pushed
  - Require review from Code Owners (optional)

#### Require status checks to pass before merging
- ✅ **Require status checks to pass before merging**
  - ✅ Require branches to be up to date before merging
  - **Required status checks:**
    - `build` (from Build and Test workflow)
    - `docker-build` (from Build and Test workflow)

#### Additional Rules (Recommended):
- ✅ **Require conversation resolution before merging**
- ✅ **Require linear history** (optional)
- ✅ **Include administrators** (optional but recommended)

## Using the GitHub CLI

If you have the GitHub CLI (`gh`) installed and authenticated, you can use the following script to configure branch protection:

```bash
#!/bin/bash

# Enable branch protection for main
gh api repos/:owner/:repo/branches/main/protection \
  --method PUT \
  -H "Accept: application/vnd.github+json" \
  -f required_status_checks='{"strict":true,"contexts":["build","docker-build"]}' \
  -f enforce_admins=true \
  -f required_pull_request_reviews='{"required_approving_review_count":1,"dismiss_stale_reviews":true}' \
  -f restrictions=null
```

Replace `:owner` and `:repo` with your GitHub username/organization and repository name.

## Verifying Protection

Once configured, the main branch will:
1. Require all pull requests to pass the "Build and Test" workflow
2. Prevent direct pushes to main
3. Require code review before merging
4. Ensure the branch is up-to-date before merging

## CI/CD Pipeline

The `.github/workflows/build.yml` workflow will automatically run on:
- Every push to `main` branch
- Every pull request targeting `main` branch

The pipeline includes:
- .NET project compilation (Release configuration)
- Running tests
- Docker image build
- Basic Docker container health check
