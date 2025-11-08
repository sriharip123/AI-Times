# Kiro IDE Configuration

This directory contains Kiro IDE-specific files and configurations.

## Directory Structure

```
.kiro/
├── specs/              # ✅ Committed to Git - Project specifications
│   └── [feature-name]/
│       ├── requirements.md
│       ├── design.md
│       └── tasks.md
├── steering/           # ✅ Committed to Git - Team conventions (if exists)
│   └── *.md
├── settings/           # ❌ Ignored - Personal IDE settings
├── cache/              # ❌ Ignored - Temporary cache
├── logs/               # ❌ Ignored - Log files
└── README.md           # ✅ This file
```

## What's Committed to Git

### ✅ `specs/` - Feature Specifications
**Purpose**: Documentation of features, requirements, and design decisions

**Why commit?**
- Provides project documentation
- Shows feature evolution over time
- Helps with team collaboration
- Useful for onboarding new developers
- Serves as a reference for implementation

**Contents**:
- `requirements.md` - User stories and acceptance criteria
- `design.md` - Technical design and architecture
- `tasks.md` - Implementation task breakdown

### ✅ `steering/` - Team Conventions (if exists)
**Purpose**: Shared coding standards and project guidelines

**Why commit?**
- Ensures consistent code style across team
- Documents project-specific conventions
- Provides context for AI-assisted development

## What's Ignored

### ❌ `settings/` - Personal Settings
- User-specific IDE preferences
- Personal keyboard shortcuts
- Individual workspace configurations
- MCP (Model Context Protocol) configurations

### ❌ `cache/` - Temporary Cache
- Cached data for faster IDE operations
- Temporary processing files
- Auto-generated content

### ❌ `logs/` - Log Files
- IDE operation logs
- Debug information
- Error traces

## Working with Specs

### Creating a New Spec
1. Use Kiro's spec creation workflow
2. Define requirements with user stories
3. Create technical design
4. Break down into implementation tasks

### Viewing Specs
- Specs are markdown files
- Can be viewed in any text editor
- Best viewed in Kiro IDE with spec support

### Updating Specs
- Specs can be updated as features evolve
- Changes are tracked in Git
- Review changes in pull requests

## Best Practices

### Do ✅
- Commit all spec files to Git
- Keep specs up-to-date with implementation
- Review spec changes in PRs
- Use specs for feature documentation

### Don't ❌
- Don't commit personal settings
- Don't commit cache or log files
- Don't commit user-specific configurations
- Don't commit temporary files

## For Team Members

When you clone this repository:
1. The `.kiro/specs/` directory will be available
2. Your personal settings will be in `.kiro/settings/` (not committed)
3. Kiro IDE will create cache/logs as needed (ignored by Git)

## Questions?

- **What is Kiro?** - An AI-powered IDE for development
- **Why specs?** - Structured way to plan and document features
- **Can I edit specs manually?** - Yes, they're just markdown files
- **Do I need Kiro to view specs?** - No, any text editor works

## Related Documentation

- [Kiro Specs Documentation](https://docs.kiro.ai/specs)
- [Contributing Guidelines](../.github/CONTRIBUTING.md)
- [Project README](../README.md)
