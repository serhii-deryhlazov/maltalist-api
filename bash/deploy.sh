#!/bin/bash

# VPS credentials
USER="root"
HOST="159.198.65.115"
REMOTE_DIR="/var/www/maltalist/api"

# Get changed files compared to origin/main (or HEAD)
changed_files=$(git diff --name-only)

if [ -z "$changed_files" ]; then
  echo "No changed files to sync."
else
  echo "Syncing changed files to $HOST..."
  for file in $changed_files; do
    # Ensure parent directory exists on remote
    remote_path="$REMOTE_DIR/$(dirname "$file")"
    ssh $USER@$HOST "mkdir -p $remote_path"

    # Copy file
    scp "$file" $USER@$HOST:"$remote_path/"
    echo "Transferred: $file"
  done
fi

# Git commit & push
echo "Committing and pushing changes..."
git add .
current_date=$(date +"%Y-%m-%d %H:%M:%S")
git commit -m "update $current_date"
git push
