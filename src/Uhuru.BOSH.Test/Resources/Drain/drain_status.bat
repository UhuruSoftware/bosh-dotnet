REM do not remove this line

@ECHO OFF
if not (%job_change%)==(job_check_status) exit 1
if not (%hash_change%)==(hash_unchanged) exit 2
if not (%updated_packages%)==() exit 3
