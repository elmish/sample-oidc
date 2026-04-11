# Contributing

Grab the source, fork it, and send a PR!

## Building

Make sure the following requirements are installed:

- [.NET SDK 10.0](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) with npm/yarn

```bash
dotnet tool restore
dotnet fsi build.fsx -t Watch
```

Then open http://localhost:8090 in your browser.
