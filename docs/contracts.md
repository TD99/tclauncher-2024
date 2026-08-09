# TCLauncher Windows Edition contracts

## Instance configuration v2

`config.json` retains all legacy fields and adds `schemaVersion: 2` plus one loader object. Supported loader types are `Vanilla`, `Fabric`, `Forge`, and `NeoForge`; the loader version is optional for vanilla. When `schemaVersion` is absent, `UseFabric` and `UseForge` are mapped in memory. Legacy files are not rewritten merely by loading them.

All configuration writes use a temporary file and atomic replacement. Instance activation uses a sibling staging directory and retains a rollback directory until activation succeeds.

## Catalog API v2

The client accepts an HTTPS JSON response with `schemaVersion: 2`, an item collection, and stable item fields: ID, slug, title, summary, description, artwork, tags, Minecraft version, loader type/version, pack version, requirements, servers, payload URL/SHA-256, featured status, and timestamps.

The catalog stores the response and ETag locally. It sends `If-None-Match`, accepts `304 Not Modified`, validates responses before use, falls back to the legacy endpoint during rollout, and finally uses the last valid cache. Network failure never leaves Home waiting indefinitely.

## Portable package v2

A `.tcl` v2 file is a ZIP container with:

- `manifest.json`: schema version, package ID, creation time, portable instance metadata, payload metadata, and per-file checksums;
- `payload.zip`: profile files selected for export;
- `config.json`: portable compatibility copy of the instance configuration;
- optional `thumb.<extension>` artwork.

The importer previews metadata, size, save inclusion, and ID conflicts before activation. It supports Replace, Import as Copy, and Cancel. Both container and payload extraction reject absolute paths, traversal, duplicate destinations, links/reparse points, invalid identifiers, oversized extraction, malformed manifests, and checksum mismatches. A failed import cannot modify the live instance.

## Backups

Default backups contain profile metadata, saves, configuration, options, and servers. Full-instance backups are opt-in. Automatic backups are created before managed updates and retain the newest three per profile; manual backups are never automatically removed. Restore extracts to staging, verifies the archive, takes a rollback snapshot, and then activates the result.

## Update manifest v1

The HTTPS update manifest contains `schemaVersion`, `version`, minimum Windows and .NET requirements, installer URL, SHA-256, expected Authenticode publisher, release notes, and mandatory status. The launcher downloads into its Updates staging directory, verifies SHA-256 and the certificate chain/publisher, asks for confirmation, and only then opens the local installer. Check or verification failure never blocks profile launch.

## Diagnostics and privacy

Rolling JSONL logs contain operation IDs and human-readable error categories. Secrets, bearer tokens, authorization values, and common token-shaped JSON properties are redacted before persistence or export. A support bundle preview lists included launcher logs, system/runtime data, selected profile metadata, and recent crash reports. It excludes account storage, tokens, saves, mods/game assets, and unrelated profiles, and is never transmitted automatically.
