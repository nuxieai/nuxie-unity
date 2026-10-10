# Generated from the owning .NET packages.lock.json files.
load("@rules_dotnet//dotnet:defs.bzl", "nuget_repo")

def _impl(_ctx):
    nuget_repo(name = "unity_nuget", packages = [
    {
        "name": "Microsoft.CodeCoverage",
        "id": "Microsoft.CodeCoverage",
        "version": "17.14.1",
        "sha512": "sha512-pmTrhfFIoplzFVbhVwUquT+77CbGH+h4/3mBpdmIlYtBi9nAB+kKI6dN3A/nV4DFi3wLLx/BlHIPK+MkbQ6Tpg==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            "net8.0": []
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    },
    {
        "name": "Microsoft.NET.Test.Sdk",
        "id": "Microsoft.NET.Test.Sdk",
        "version": "17.14.1",
        "sha512": "sha512-HJKqKOE+vshXra2aEHpi2TlxYX7Z9VFYkr+E5rwEvHC8eIXiyO+K9kNm8vmNom3e2rA56WqxU+/N9NJlLGXsJQ==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            "net8.0": [
                "Microsoft.CodeCoverage",
                "Microsoft.TestPlatform.TestHost"
            ]
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    },
    {
        "name": "Microsoft.TestPlatform.ObjectModel",
        "id": "Microsoft.TestPlatform.ObjectModel",
        "version": "17.14.1",
        "sha512": "sha512-xTP1W6Mi6SWmuxd3a+jj9G9UoC850WGwZUps1Wah9r1ZxgXhdJfj1QqDLJkFjHDCvN42qDL2Ps5KjQYWUU0zcQ==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            "net8.0": [
                "System.Reflection.Metadata"
            ]
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    },
    {
        "name": "Microsoft.TestPlatform.TestHost",
        "id": "Microsoft.TestPlatform.TestHost",
        "version": "17.14.1",
        "sha512": "sha512-d78LPzGKkJwsJXAQwsbJJ7LE7D1wB+rAyhHHAaODF+RDSQ0NgMjDFkSA1Djw18VrxO76GlKAjRUhl+H8NL8Z+Q==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            "net8.0": [
                "Microsoft.TestPlatform.ObjectModel",
                "Newtonsoft.Json"
            ]
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    },
    {
        "name": "Newtonsoft.Json",
        "id": "Newtonsoft.Json",
        "version": "13.0.3",
        "sha512": "sha512-HrC5BXdl00IP9zeV+0Z848QWPAoCr9P3bDEZguI+gkLcBKAOxix/tLEAAHC+UvDNPv4a2d18lOReHMOagPa+zQ==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            ".NETStandard,Version=v2.1": [],
            "net8.0": []
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    },
    {
        "name": "System.Collections.Immutable",
        "id": "System.Collections.Immutable",
        "version": "8.0.0",
        "sha512": "sha512-AurL6Y5BA1WotzlEvVaIDpqzpIPvYnnldxru8oXJU2yFxFUy3+pNXjXd1ymO+RA0rq0+590Q8gaz2l3Sr7fmqg==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            "net8.0": []
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    },
    {
        "name": "System.Reflection.Metadata",
        "id": "System.Reflection.Metadata",
        "version": "8.0.0",
        "sha512": "sha512-ptvgrFh7PvWI8bcVqG5rsA/weWM09EnthFHR5SCnS6IN+P4mj6rE1lBDC4U8HL9/57htKAqy4KQ3bBj84cfYyQ==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            "net8.0": [
                "System.Collections.Immutable"
            ]
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    },
    {
        "name": "xunit",
        "id": "xunit",
        "version": "2.9.3",
        "sha512": "sha512-TlXQBinK35LpOPKHAqbLY4xlEen9TBafjs0V5KnA4wZsoQLQJiirCR4CbIXvOH8NzkW4YeJKP5P/Bnrodm0h9Q==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            "net8.0": [
                "xunit.analyzers",
                "xunit.assert",
                "xunit.core"
            ]
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    },
    {
        "name": "xunit.abstractions",
        "id": "xunit.abstractions",
        "version": "2.0.3",
        "sha512": "sha512-pot1I4YOxlWjIb5jmwvvQNbTrZ3lJQ+jUGkGjWE3hEFM0l5gOnBWS+H3qsex68s5cO52g+44vpGzhAt+42vwKg==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            "net8.0": []
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    },
    {
        "name": "xunit.analyzers",
        "id": "xunit.analyzers",
        "version": "1.18.0",
        "sha512": "sha512-OtFMHN8yqIcYP9wcVIgJrq01AfTxijjAqVDy/WeQVSyrDC1RzBWeQPztL49DN2syXRah8TYnfvk035s7L95EZQ==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            "net8.0": []
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    },
    {
        "name": "xunit.assert",
        "id": "xunit.assert",
        "version": "2.9.3",
        "sha512": "sha512-/Kq28fCE7MjOV42YLVRAJzRF0WmEqsmflm0cfpMjGtzQ2lR5mYVj1/i0Y8uDAOLczkL3/jArrwehfMD0YogMAA==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            "net8.0": []
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    },
    {
        "name": "xunit.core",
        "id": "xunit.core",
        "version": "2.9.3",
        "sha512": "sha512-BiAEvqGvyme19wE0wTKdADH+NloYqikiU0mcnmiNyXaF9HyHmE6sr/3DC5vnBkgsWaE6yPyWszKSPSApWdRVeQ==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            "net8.0": [
                "xunit.extensibility.core",
                "xunit.extensibility.execution"
            ]
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    },
    {
        "name": "xunit.extensibility.core",
        "id": "xunit.extensibility.core",
        "version": "2.9.3",
        "sha512": "sha512-kf3si0YTn2a8J8eZNb+zFpwfoyvIrQ7ivNk5ZYA5yuYk1bEtMe4DxJ2CF/qsRgmEnDr7MnW1mxylBaHTZ4qErA==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            "net8.0": [
                "xunit.abstractions"
            ]
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    },
    {
        "name": "xunit.extensibility.execution",
        "id": "xunit.extensibility.execution",
        "version": "2.9.3",
        "sha512": "sha512-yMb6vMESlSrE3Wfj7V6cjQ3S4TXdXpRqYeNEI3zsX31uTsGMJjEw6oD5F5u1cHnMptjhEECnmZSsPxB6ChZHDQ==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            "net8.0": [
                "xunit.extensibility.core"
            ]
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    },
    {
        "name": "xunit.runner.utility",
        "id": "xunit.runner.utility",
        "version": "2.9.3",
        "sha512": "sha512-cAUw6GadBR19A9/345e3BFiAkhN9P5xPrxiZgks0xdRv+DxdIWiizE5vjyExKNyFzsm+r1jDhccpUyojBDT7OA==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            "net8.0": [
                "xunit.abstractions"
            ]
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    },
    {
        "name": "xunit.runner.visualstudio",
        "id": "xunit.runner.visualstudio",
        "version": "3.1.4",
        "sha512": "sha512-5mj99LvCqrq3CNi06xYdyIAXOEh+5b33F2nErCzI5zWiDdLHXiPXEWFSUAF8zlIv0ZWqjZNCwHTQeAPYbF3pCg==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            "net8.0": []
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    }
])

unity_nuget = module_extension(implementation = _impl)
