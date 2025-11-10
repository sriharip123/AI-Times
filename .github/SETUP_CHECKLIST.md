# GitHub Actions Setup Checklist

Use this checklist to complete the GitHub Actions setup for your repository.

## ✅ Setup Steps

### 1. ~~Update Status Badges~~ ✅ DONE
- [x] Open `README.md`
- [x] Replace `YOUR_USERNAME` with your GitHub username
- [x] Replace `YOUR_REPO` with your repository name
- [x] Save the file

✅ Updated to:
```markdown
![Tests](https://github.com/sriharip123/AI-Times/actions/workflows/test.yml/badge.svg)
```

### 2. Commit and Push Workflow Files
```bash
git add .github/
git add README.md
git commit -m "ci: add GitHub Actions workflows for automated testing"
git push origin main
```

### 3. Verify Workflows Are Active
- [ ] Go to your repository on GitHub
- [ ] Click the "Actions" tab
- [ ] You should see the workflows listed
- [ ] If not, check that files are in `.github/workflows/`

### 4. Test the Workflow
- [ ] Create a new branch: `git checkout -b test/ci-workflow`
- [ ] Make a small change (e.g., add a comment to README)
- [ ] Commit and push: `git push origin test/ci-workflow`
- [ ] Create a pull request on GitHub
- [ ] Watch the workflow run automatically
- [ ] Verify tests pass

### 5. Configure Branch Protection (Recommended)
- [ ] Go to Settings → Branches on GitHub
- [ ] Click "Add rule"
- [ ] Branch name pattern: `main`
- [ ] Check "Require status checks to pass before merging"
- [ ] Search for and select these required checks:
  - "Build and Test" (from test.yml workflow)
  - "Build and Test" (from ci.yml workflow)
  - "Code Quality Checks" (from ci.yml workflow)
- [ ] Check "Require branches to be up to date before merging"
- [ ] Click "Create" or "Save changes"

**Note:** You can require all checks or just the test.yml workflow depending on your quality requirements.

### 6. Optional: Enable Notifications
- [ ] Go to Settings → Notifications
- [ ] Configure how you want to be notified of workflow failures
- [ ] Consider enabling email notifications for failed workflows

## 🎯 What You Get

After completing setup:

✅ **Automated Testing**
- Tests run on every PR
- Tests run on every push to main
- 362 tests executed automatically
- Test results published with detailed reporting

✅ **Quality Gates**
- PRs can't be merged if tests fail
- Main branch always has passing tests
- Immediate feedback on code changes

✅ **Visibility**
- Status badges show build health
- Test results in PR checks
- Detailed logs in Actions tab

✅ **Artifacts**
- Test results saved for 30 days
- Easy to review past runs
- Downloadable test reports

## 📊 Monitoring

### Check Workflow Status

**Using GitHub CLI:**
```bash
# View recent workflow runs
gh run list --workflow=test.yml

# View specific run details
gh run view <run-id>

# Watch a workflow run in real-time
gh run watch
```

**View in Browser:**
1. Go to `https://github.com/sriharip123/AI-Times/actions`
2. Click on any workflow run
3. Review logs and test results
4. Download test artifacts if needed

**Status Badges:**
- Check README.md for real-time workflow status
- Green badge = passing, Red badge = failing

## 🔧 Troubleshooting

### Workflow not appearing?
```bash
# Verify files are in correct location
ls -la .github/workflows/

# Check YAML syntax
cat .github/workflows/test.yml

# Verify workflow files are committed
git ls-files .github/workflows/
```

**Common causes:**
- Files not committed to repository
- YAML syntax errors
- GitHub Actions not enabled in repository settings

### Tests failing in CI but passing locally?
1. Check the Actions tab for detailed logs
2. Look for error messages in test output
3. Compare with local test results
4. Verify .NET version matches (9.0.x)
5. Check for environment-specific issues:
   - Service connectivity (Ollama, ScyllaDB)
   - Configuration differences
   - Missing environment variables

**Debug steps:**
```bash
# Run tests exactly as CI does
dotnet tool restore
dotnet restore
dotnet build --no-restore --configuration Release
dotnet test --no-build --configuration Release --verbosity normal
```

### Workflow runs but tests fail?
1. Review test output in the Actions tab
2. Check if services are properly configured
3. Verify test data and fixtures are correct
4. Run diagnostic commands locally:
   ```bash
   dotnet run --project JSON-Whisperer -- --health-check
   dotnet run --project JSON-Whisperer -- --validate-config
   ```

### Need to disable temporarily?
1. Go to Actions tab
2. Click on the workflow
3. Click "..." menu
4. Select "Disable workflow"

### Artifacts not uploading?
- Check artifact retention settings (default: 30 days)
- Verify test results are generated (*.trx files)
- Check workflow logs for upload errors

## 📚 Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET Testing in CI/CD](https://docs.microsoft.com/en-us/dotnet/core/testing/)
- [Workflow Syntax](https://docs.github.com/en/actions/reference/workflow-syntax-for-github-actions)

## ✅ Validation Steps

After completing setup, verify everything works:

### 1. Verify Workflows Are Active
```bash
# Check workflow files exist
ls -la .github/workflows/

# Verify they're committed
git log --oneline .github/workflows/
```

### 2. Test Workflow Execution
```bash
# Create test branch
git checkout -b test/verify-ci

# Make a small change
echo "# CI Test" >> test-ci.md

# Commit and push
git add test-ci.md
git commit -m "test: verify CI workflow"
git push origin test/verify-ci
```

Then:
- Go to GitHub and create a PR
- Watch workflows run automatically
- Verify all checks pass
- Check test results are published

### 3. Verify Status Badges
- Open README.md in GitHub
- Confirm status badges display correctly
- Click badges to view workflow runs

### 4. Test Diagnostic Commands Locally
```bash
# Verify services are operational
dotnet run --project JSON-Whisperer -- --health-check

# Validate configuration
dotnet run --project JSON-Whisperer -- --validate-config
```

## ✨ Next Steps

After setup is complete:

1. **Create your first PR** to see the workflow in action
2. **Review test results** to understand the reporting
3. **Share with team** so everyone knows about the CI process
4. **Monitor regularly** to catch issues early
5. **Set up notifications** for workflow failures

## 🎉 You're Done!

Once all checkboxes are complete and validation passes, your CI/CD pipeline is fully operational!

---

**Questions?** Check `.github/workflows/README.md` or create an issue.
