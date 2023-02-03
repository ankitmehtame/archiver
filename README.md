[![Docker Image CI](https://github.com/ankitmehtame/archiver/actions/workflows/docker-image.yml/badge.svg)](https://github.com/ankitmehtame/http-forwarder/actions/workflows/docker-image.yml)
![Docker Image Version (tag latest semver)](https://img.shields.io/docker/v/ankitmehtame/archiver/latest?color=blue)

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
or
```
archiver --source "C:\Source" --extensions ".mp4|.mpeg" --retention 15 --max 45 --format "*yyyy-MM-dd_HH-mm-ss" --delete
```
or
```
docker run --rm -it  -v "//d//Media/Videos:/source" archiver --source /source --extensions ".mp4|.mpeg" --retention 30 --max 60 --format "*yyyy-MM-dd_HH-mm-ss" --delete --debug --demo --recurse
```

#### Options
_--extensions_ | _-e_: File extensions to archive, example: ".mp4|.avi|.mpeg", default is ".*"

_--retention_ | _-r_: Number of days to retain locally, after which files will be archived. Default values is 15.

_--max_ | _-m_: Maximum number of days to look at. Default value is 36500.

_--format_ | _-f_: File name date format. Date can only be at the front or the end. Example: "*yyyy-MM-dd_HH-mm-ss"

_--debug_: Enables debug logging (information by default)

_--demo_: Enables demo mode where no changes are made

_--recurse_: Recurse sub directories in source path. Default value is false.

_--delete_ | _-D_: Deletes instead of archiving. Default value is false.
