# ToStorage
Pipe stuff to storage.

Takes `stdin` and writes it to Azure Blob Storage. Uploads two blobs by default: a permalink based on the current UTC time and updates a "latest" blob. 

## Example

### Execute

```
ping google.com | ToStorage -s CONNECTION_STRING -c CONTAINER
```

### Output

```
Initializing... done.
Uploading the blob to 2016.02.01.00.23.47.txt... done.
Setting the content type... done.
Updating latest.txt to the latest blob... done.

Direct: https://ACCOUNT.blob.core.windows.net/CONTAINER/2016.02.01.00.23.47.txt
Latest: https://ACCOUNT.blob.core.windows.net/CONTAINER/latest.txt
```

## Usage

```
Knapcode.ToStorage 0.0.0.0
(no copyright)

  -s, --connection-string    Required. The connection string for Azure Storage.

  -c, --container            Required. The container name.

  -f, --path-format          (Default: {0}.txt) The format to use when building
                             the path.

  -t, --content-type         (Default: text/plain) The content type to set on
                             the blob.

  -l, --update-latest        (Default: true) Whether or not to set the 'latest'
                             blob.

  -d, --update-direct        (Default: true) Whether or not to set the 'direct'
                             blob.

  -u, --only-unique          (Default: false) Whether or not to only update the
                             'latest' blob if the 'latest' blob will change.

  --help                     Display this help screen.

  --version                  Display version information.
```
