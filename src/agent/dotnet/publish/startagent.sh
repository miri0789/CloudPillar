#!/bin/bash

export ARCHITECTURE=$1
export DEVICE_CONNECTION_STRING=$2

if [ -z "$DEVICE_CONNECTION_STRING" ]; then
    echo "Usage: startagent.sh ARCHITECTURE DEVICE_CONNECTION_STRING"
    exit 1
fi

# Shift the arguments to exclude the first one
shift

# Run the self-contained deployment
"$ARCHITECTURE/jnjiotagent" "$@"
