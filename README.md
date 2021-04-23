# SFCC Tools

**NOTE: this is pre-release software; a work in-progress**

Library and collections of tools for working with SFCC realms, instances and other services (OCAPI, etc).

## Install

TODO: Non-dev install instructions

## Development

### Pre-req

- Dotnet Core 3.1
- Docker (for building and for test database if required)

#### Recommendations for Editors

- Jetbrains Rider
    - https://www.jetbrains.com/rider/
- VSCode with Omnisharp (C#) extension
    - https://code.visualstudio.com/docs/languages/csharp

### Building

Build the solution using the dotnet toolset or the above editors

```
docker-compose up -d # if you want a database (needed for the BI tools)
dotnet build
# copy appsettings.json.example to appsettings.json and change relevant settings
dotnet run --project src/SFCCTools.Console/SFCCTools.CLI
dotnet run --project src/SFCCTools.Console/SFCCTools.Jobs
```

### Testing

```
dotnet test
```

To exclude functional tests that require a database or SFCC instance (they will fail otherwise). Use
any combination of these test trait filters.

```
dotnet test --filter 'Category!=RequiresDatabase & Category!=RequiresInstance'
```

To test with an instance ensure that the `testdata/` folder has a valid `dw.json` file (see sample).

To test with a database ensure a postgres database is available (use the included `docker-compose.yml`
to use docker-compose to start up a ready-to-use db). The `appsettings.json` file already in `testdata/`
points to this instance.

Note: The given database will be automatically migrated at startup (and created if needed and the necessary permissions
are available).

## Environment Variables

You can also use environment variables to provide the necessary configuration for test, development and production (recommended) use:

```
# Database properties
ConnectionStrings__Default=Host=localhost;Database=bi;Username=postgres;Password=password

# Instance Properties
Server=dev04-na01-xyz.demandware.net
ClientID=xyz
ClientSecret=foobar
CodeVersion=foobar
```
