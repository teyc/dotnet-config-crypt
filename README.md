# config-crypt

A .NET Global Tool for encrypting and decrypting secrets in JSON configuration files.

## Overview

`config-crypt` is a secure and easy-to-use command-line tool that helps you manage sensitive information in your JSON configuration files. It automatically detects and encrypts secrets like API keys, passwords, and connection strings while leaving non-sensitive data in plaintext for readability.

## Features

- Automatically encrypts values with keys containing "password", "key", "secret" (configurable)
- Automatically encrypts values containing sensitive information
- Strong encryption using AES-GCM (authenticated encryption)
- Supports nested JSON objects and arrays
- Exclude paths from encryption using JSONPath
- Simple command-line interface
- Installable as a .NET global tool

## Installation

```bash
dotnet tool install --global config-crypt
```

## Usage

### Encrypting a JSON file

```bash
config-crypt crypt filename.json [filename.encrypted.json]
```

The output file is optional. If not specified, it will default to `filename.encrypted.json`.

### Decrypting a JSON file

```bash
config-crypt decrypt filename.encrypted.json [filename.json]
```

The output file is optional. If not specified, it will be inferred from the input file name by:
- Replacing `.encrypted.json` with `.json` if present
- Otherwise, using `filename.decrypted.json`

## Configuration

Configuration is stored in `~/.config-crypt/config.json` and contains:

```json
{
  "AesKey": "base64-encoded-key",
  "Secrets": ["key", "password", "secret"],
  "Exclude": ["$.publicData", "$.anotherPath"]
}
```

- `AesKey`: A base64-encoded 256-bit AES key. Automatically generated if not present.
- `Secrets`: A list of strings to match against property names and values for encryption.
- `Exclude`: A list of JSONPath expressions indicating paths that should not be encrypted.

## How It Works

1. The tool scans your JSON file for properties containing sensitive information.
2. When a sensitive value is found (based on the property name or value content), it's encrypted.
3. Encrypted values are stored in the format: `@config-crypt(encryptedvalue)`.
4. During decryption, any value with this format is decrypted back to its original form.

## Example

**Original JSON:**
```json
{
  "application": {
    "name": "MyApp",
    "apiKey": "secret-api-key-123",
    "database": {
      "connectionString": "Server=mydb;Password=dbpass123"
    }
  }
}
```

**Encrypted JSON:**
```json
{
  "application": {
    "name": "MyApp",
    "apiKey": "@config-crypt(ABCD1234...)",
    "database": {
      "connectionString": "@config-crypt(WXYZ5678...)"
    }
  }
}
```

## Security Considerations

- The encryption key is stored in `~/.config-crypt/config.json`. Keep this file secure!
- The tool uses AES-GCM, which provides both confidentiality and authenticity.
- Each encryption operation uses a unique nonce (number used once) to prevent replay attacks.

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

## License

MIT