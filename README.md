# ToStorage 

Pipe stuff to storage.

Takes `stdin` and writes it to Azure Blob Storage. Uploads two blobs by default: a permalink based on the current UTC time and updates a "latest" blob. 

[![Build status](https://ci.appveyor.com/api/projects/status/2nqj5vk1w1jrjf7i?svg=true)](https://ci.appveyor.com/project/joelverhagen/tostorage)

## Example

### Execute

```
ping google.com | ToStorage -s CONNECTION_STRING -c CONTAINER
```

### Output

```
Initializing... done.
Uploading the blob at '2016.09.21.14.52.23.8126950.txt'... done.
Setting the content type of '2016.09.21.14.52.23.8126950.txt'... done.
Copying the direct blob to the latest blob at 'latest.txt'... done.

Direct: https://ACCOUNT.blob.core.windows.net/CONTAINER/2016.09.21.14.52.23.8126950.txt
Latest: https://ACCOUNT.blob.core.windows.net/CONTAINER/latest.txt
```

## Usage

```
Knapcode.ToStorage.Tool: send standard input (stdin) to Azure Blob storage.

Usage: ToStorage [options]

Options:
  -s|--connection-string  (required) The connection string for Azure Storage.
  -c|--container          (required) The container name.
  -f|--path-format        The format to use when building the path. Default: '{0}.txt'.
  -t|--content-type       The content type to set on the blob. Default: 'text/plain'.
  --no-latest             Don't upload the latest blob.
  --no-direct             Don't upload the direct blob.
  -u|--only-unique        Only upload if the current upload is different than the lastest blob.
  -h|--help               Show help information.
```
