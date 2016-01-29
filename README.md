# ToStorage
Pipe stuff to storage.

Takes `stdin` and writes it to Azure Blob Storage. Uploads two blobs by default: a permalink based on the current UTC time and updates a "latest" blob. 

## Example

### Execute

```
ping google.com | ToStorage -a ACCOUNT -k KEY -c CONTAINER
```

### Output

```
Initializing... done.
Uploading the blob to 2016.01.29.15.48.16.txt... done.
Setting the content type... done.
Updating latest.txt to the latest blob... done.

Direct: https://ACCOUNT.blob.core.windows.net/CONTAINER/2016.01.29.15.48.16.txt
Latest: https://ACCOUNT.blob.core.windows.net/CONTAINER/latest.txt
```

## Usage

```
Knapcode 0.0.0.0
(no copyright)

  -k, --key                  The key used to access Azure Storage.

  -a, --account              The Azure Storage account name.

  -s, --connection-string    The connection string for Azure Storage.

  -c, --container            Required. The container name.

  -f, --path-format          Required. (Default: {0}.txt) The format to use
                             when building the path.

  -t, --content-type         (Default: text/plain) The content type to set on
                             the blob.

  -l, --update-latest        (Default: true) Whether or not to set the 'latest'
                             blob.

  --help                     Display this help screen.

  --version                  Display version information.
```
