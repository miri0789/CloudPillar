#!/bin/bash

export ARCHITECTURE=${1:-linux-x64}

# Shift the arguments to exclude the first one
shift

# Run the self-contained deployment
"../../$ARCHITECTURE/jnjiotagent" "$@"
