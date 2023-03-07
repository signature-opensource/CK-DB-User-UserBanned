# CK.DB.User.UserPassword.Banned

This package is based on `CK.DB.User.UserPassword` and `CK.DB.User.UserBanned`.

It adds banishment rules for basic authentication.

This `CK.DB.User.UserPassword.Banned.Package` injects code into `sAuthUserOnLogin` procedure (from CK.DB.Auth package). When a login failed and after incrementation of the FailedAttemptCount from the CK.tUserPassword table, it applies the followings banishment rules:
- If user failed login more 3 times, he is banned for 15 minutes.
- If user failed login more 5 times, he is banned for 2 hours.
- If user failed login more 8 times, he is banned for 24 hours.
