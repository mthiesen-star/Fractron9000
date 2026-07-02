## Release Process

Create a tag with:
```bash
git tag -a v0.5.0 -m "Fractron9000 0.5.0 initial github release"
git push origin v0.5.0
```

Then use the github UI to manually create a new release for that tag. The site will be automatically deployed by `.github/workflows/pages.yml`.
