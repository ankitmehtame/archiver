# archiver
Archives media

#### To run
```
archiver --source "C:\Source" --destination "M:\Destination" --extensions ".mp4|.mpeg" --retention 15 --format "*yyyy-MM-dd_HH-mm-ss"
```
or
```
archiver -s "C:\Source" -d "M:\Destination" -e ".mp4|.mpeg" -r 15 -f "*yyyy-MM-dd_HH-mm-ss"
```

#### Options
_extensions_: File extensions to archive, example: ".mp4|.avi|.mpeg", default is ".*"

_retention_: Number of days to retain locally, after which files will be archived

_format_: File name date format. Date can only be at the front or the end. Example: "*yyyy-MM-dd_HH-mm-ss"
