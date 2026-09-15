# Project decisions

| Decision | Reason |
| --- | --- |
| WPF on .NET 8 | Native Windows integration with a maintainable desktop UI. |
| Registry Run keys and Startup folders first | These are common, user-visible startup sources and support reliable management. |
| Application-managed disabled store | Makes disable/re-enable explicit and reversible without relying on undocumented Windows state. |
| Elevate only when required | User-level operations remain frictionless while machine-level changes can still be performed. |
