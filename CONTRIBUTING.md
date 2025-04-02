
## Development

### Building from source

```bash
git clone https://github.com/yourusername/config-crypt.git
cd config-crypt
dotnet build
```

### Running tests

```bash
dotnet test
```

### Installing the development version

```bash
dotnet pack
dotnet tool install --global --add-source ./nupkg config-crypt
```
