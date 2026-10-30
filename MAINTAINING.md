# Maintaining Our.Umbraco.Skipper

## Branch model

| Branch | Package line | Umbraco | .NET | State |
|---|---|---|---|---|
| `master` | 1.x | 9–13 | net5.0 / net6.0 | frozen |
| `v17` | 17.x | 17 | net10.0 | active |
| `v18` | 18.x | 18 | net10.0 | active |

- `master` is the 1.x line and is frozen. Do not merge v17/v18 work into it.
- Each supported Umbraco major gets its own branch (`v19`, …), cut from the previous major's branch and then adapted.
- Never delete or force-push these branches. A repository ruleset enforces this.
- Port fixes across majors with `git cherry-pick`, not by merging one major branch into another.

## Versioning

- The package major follows the Umbraco major: `17.0.0` for Umbraco 17, `18.0.0` for Umbraco 18.
- Target the Umbraco major's .NET version (`net10.0` for 17/18, `net8.0` for a future 13 line).
- Bound the Umbraco dependency to its major, for example `[17.0.0,18.0.0)` and `[18.0.0,19.0.0)`. This makes NuGet refuse the package on the wrong Umbraco major instead of failing at runtime.
- Do not reference `Umbraco.Cms.Web.BackOffice`. It was discontinued after 13.x and Skipper does not use it.

## Release

1. Bump `<VersionPrefix>` in `src/Our.Umbraco.Skipper.csproj` on the version branch.
2. Commit and push the branch.
3. Tag the commit: `git tag v17.1.0 && git push origin v17.1.0`.
4. The `Publish to NuGet` workflow builds the package, verifies that the tag matches `VersionPrefix`, and publishes to nuget.org.
5. Create a GitHub release from the tag with release notes.

## NuGet publishing

Preferred: **Trusted Publishing** (no long-lived API key).

1. On nuget.org: **Account → Trusted Publishing → Add policy**.
   - Repository owner: `ZioTino`
   - Repository: `Our.Umbraco.Skipper`
   - Workflow file: `publish.yml`
   - Environment: leave empty
   - Scope: allow pushing new packages and new versions of `Our.Umbraco.Skipper`
2. In GitHub: **Settings → Secrets and variables → Actions → New repository secret**
   - `NUGET_USER` = your nuget.org username (profile name), not your email address.
3. The workflow requests a short-lived key with `NuGet/login@v1` and pushes with it.

Fallback: add a `NUGET_API_KEY` repository secret (nuget.org → API keys). The workflow uses it instead of OIDC when it is present. Prefer trusted publishing; the API key is long-lived.

Package ownership on nuget.org is separate from GitHub. Add the new maintainer under **nuget.org → Manage package → Owners**.

If the GitHub repository is transferred, update the trusted publishing policy to the new owner/repository.

## Testing

- Build: `dotnet build src/Our.Umbraco.Skipper.csproj -c Release`
- Runtime test: create an Umbraco site on the matching major, add a project reference to the branch, and seed:

  ```text
  Home
    Products
      Category
        Product One
        Product Two
  ```

  - With `Umbraco:Skipper: { "HideSkipperWork": true, "Aliases": ["category"] }`:
    category has no usable URL (HTTP 404), product is `/products/product-one/`.
  - With `{ "HideSkipperWork": false, "Aliases": ["products", "category"] }`:
    category is `/category/`, product is `/product-one/`.

The v17 and v18 ports were verified this way against Umbraco 17.8.0 and 18.3.0 with SQLite.

## Known issue

`SkipperContentSavingNotification` throws `ArgumentNullException("UmbracoContext")` when content is saved outside an HTTP request (imports, migrations, background jobs). Consider returning early when `TryGetUmbracoContext` returns `false`.
