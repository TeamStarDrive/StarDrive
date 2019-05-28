#!/bin/bash -ex

# Create a Sentry API key with project:write permissions from under your Sentry organization's settings.
# Set the following environment variables.

# SENTRY_API_KEY         (Required) API Key for the target Sentry account
# SENTRY_ORGANIZATION    (Optional) Slug (aka name) for the Sentry organization.
#                           If not provided, this script assumes Sentry matches Bitbucket organization.
#                           Example: "demo"
# SENTRY_PROJECT         (Optional) Slug (aka name) for the Sentry project.
#                           If not provided, this script assumes Sentry matches Bitbucket repository.
#                           Example: "ZeroDivisionError"
# BITBUCKET_COMMIT       (Provided) Commit SHA that triggered the build.

if [ -z "${SENTRY_ORGANIZATION}" ]; then
    SENTRY_ORGANIZATION=${BITBUCKET_REPO_OWNER}
fi
if [ -z "${SENTRY_PROJECT}" ]; then
    SENTRY_PROJECT=${BITBUCKET_REPO_SLUG}
fi

curl https://app.getsentry.com/api/0/projects/${SENTRY_ORGANIZATION}/${SENTRY_PROJECT}/releases/ \
-H "Authorization: Bearer ${SENTRY_API_KEY}" \
-X POST \
-H "Content-Type:application/json" \
-d '
{
	"version": "develop_1687",
	"refs": [{
		"repository": "sd-blackbox",
		"commit": "093d4a5"
	}],
	"projects": ["\"${SENTRY_PROJECT}\""]
}
'