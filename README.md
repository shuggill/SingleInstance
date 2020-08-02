# SingleInstance
Windows Serivce to limit instances of specified processes

If you have a requirement to limit the number of instances of a particular Windows process (for example to stop a user opening multiple windows) this code runs as a Windows Service and continuously monitors the process list.  As soon as it finds (currently more than 1) instance it kills the most recently started one.

# Configuration
The service will run in either a configurable mode where the process list can be specified as a comma separated list in App.config, or in a locked down mode with the process list hard-coded (to prevent users from tampering).  The latter mode is achieved by compiling with the symbol LOCKEDDOWN.

# Logging
The service uses log4net to write daily logs indicating when:
* The service starts and stops
* What the configured list of processes to monitor is
* Any time it detects multiple instances and which process

All logging can be configured in log4net.config.
