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
_--extensions_ | _-e_: File extensions to archive, example: ".mp4|.avi|.mpeg", default is ".*"

_--retention_ | _-r_: Number of days to retain locally, after which files will be archived

_--format_ | _-f_: File name date format. Date can only be at the front or the end. Example: "*yyyy-MM-dd_HH-mm-ss"

_--debug_: Enables debug logging (information by default)

_--demo_: Enables demo mode where no changes are made
