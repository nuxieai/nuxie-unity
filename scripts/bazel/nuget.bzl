# Generated from the owning .NET packages.lock.json files.
load("@rules_dotnet//dotnet:defs.bzl", "nuget_repo")

def _impl(_ctx):
    nuget_repo(name = "unity_nuget", packages = [
    {
        "name": "Microsoft.CodeCoverage",
        "id": "Microsoft.CodeCoverage",
        "version": "17.14.1",
        "sha512": "sha512-J/ZZlEKHfgDMZSUtDbsaccoLC1rrETvotQMMf4n6TbfLxytdYTP3dbKxaE5x2D+bXuwoESZeSgyd24Ao3prhZg==",
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
        "sha512": "sha512-StGq/XXxPRDVTuHokqg1QDNWNo/NCHBF+7EAQWIt1revlYeTM/zdTEp193L1Sqtd0wIUhuT/7AiIzJKQ3HNxsw==",
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
        "sha512": "sha512-+jiozvIIOnkO3pnZf4QIuswoZBbj8e4+RTx11KkfDbrhnFLYwSfuXkI0p/7xQIBP6XWfZvEsmfCPrzy9zIo+3Q==",
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
        "sha512": "sha512-/V3Acu/lqnYDInFcadS5g+1Pd19Qd+xrdQqQ4t08i4euP9qWH6XGoyz6YldTBa6EUf2b/8cAoD8cLjzoCGATBA==",
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
        "sha512": "sha512-mbJSvHfRxfX3tR/U6n1WU+mWHXswYc+SB/hkOpx8yZZe68hNZGfymJu0cjsaJEkVzCMqePiU6LdIyogqfIn7kg==",
        "sources": [
            "https://api.nuget.org/v3/index.json"
        ],
        "dependencies": {
            "netstandard2.1": [],
            "net8.0": []
        },
        "targeting_pack_overrides": [],
        "framework_list": []
    },
    {
        "name": "System.Collections.Immutable",
        "id": "System.Collections.Immutable",
        "version": "8.0.0",
        "sha512": "sha512-BXqVkcIrhimvvem6q2ChWkuW6XYYirvb6FlhvuwaMoBqBdpcr4nehJBKP65Tw40UqcUM6oDoODsecM0yjZ6AUw==",
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
        "sha512": "sha512-+6sMdkJjee0B6nm3AlBBl7cQaI0oPniLvvkrkFhmEN3fo/hGONaFdwpAaO+GRTlbZe4kRZzFwU7kSXQW0RyJxg==",
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
        "sha512": "sha512-3/ayVPC7NQWQENR5REbOgXYsbhoJsmpnxQa5pO4lxbjGbckOs62nsm4kLErzc8ng7V5Xz08uwVjMqaZGJiXCrg==",
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
        "sha512": "sha512-PKJri5f0qEQPFvgY6CZR9XG8JROlWSdC/ZYLkkDQuID++Egn+yWjB+Yf57AZ8U6GRlP7z33uDQ4/r5BZPer2JA==",
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
        "sha512": "sha512-Yy9tOAzVncE1avA2GrOJoUxTmT6fQJOcqxsg9f53Ox7Y7V//BtQ4GV9zg7qnqMzyzymabriJ55SGKb1HKbPOOA==",
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
        "sha512": "sha512-wfqwCKAhSWGy9P/dPqDGSIBnPW3sUJ49MEfcTqNF+5BgJwjwtHb9SE7ajYZuR8ymTd8dwxoEGnlJHiejbgDv9w==",
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
        "sha512": "sha512-cv2sO37qJkIbBL3fXDIn3EPQ2zK8LQ6FkMJNnn1xc9n8mo3ik0URA4MfUNCmwDDCx83ZiJeRrJ0y1ykasojNJg==",
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
        "sha512": "sha512-S0a+jmIF/DraKuJ+FfWbqXMwvpcKxjP3GdrQzz5pr3GYtgII2XfDdAhkU/5VIWqWon2R6Q31X/9sTGaU+koDaQ==",
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
        "sha512": "sha512-IidoBSrGw/KhWzZsKXIcStohj/oRFZizbWeUv+0hOFLeMJMegSW5QoGNzmjQuF8BuRtCyPQQukWSYdNnnfPAkA==",
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
        "sha512": "sha512-L2zlPa7Ci/Awf5LdeTOvKOanev1bB6xV2Gxbrc+EDDN1hO/j0AIbu5PM8lXgkX69/8xaaex7zrxHZd8gcz2ilQ==",
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
        "sha512": "sha512-kBchYhMXhe6mbAAkCuh2wrLbxDYdj3VzNOJ3m1Q9LoCEJ6ePsFC1S3vsjj/QlY6aaQ8HeRip816tlwP0lLtb3Q==",
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
