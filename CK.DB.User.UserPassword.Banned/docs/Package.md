Automatic banishment after repeated basic-authentication failures.

Based on CK.DB.User.UserPassword and CK.DB.User.UserBanned. Brings no schema of its own: it transforms
`CK.sAuthUserOnLogin` so that, once the failed attempt count has been incremented, the user is banned
for 15 minutes from the 4th failure, 2 hours from the 6th, and 24 hours from the 9th.

The ban is issued by the System user under the single reason "UserPassword.TooManyAttempt", so
successive failures move one interval instead of accumulating rows. While a ban holds, the login is
refused earlier and the attempt counter stops climbing.
