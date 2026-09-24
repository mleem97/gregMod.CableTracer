# Quickstart — gregMod.CableTracer

> Copy `gregMod.CableTracer.dll` to `Data Center/Mods/`.

Repo: [https://github.com/mleem97/gregMod.CableTracer](https://github.com/mleem97/gregMod.CableTracer) · Version: `0.1.0` · License: Apache-2.0.

## 1. Clone

```bash
git clone https://github.com/mleem97/gregMod.CableTracer.git
cd gregMod.CableTracer
```

## 2. Build / Start

Depending on your tech stack, choose **one** path:

```bash
# .NET
dotnet build -c Release
dotnet run --project src/

# Node / pnpm
pnpm install
pnpm build
pnpm start

# Python
python -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
python -m <modul>
```

## 3. Test

```bash
dotnet test            # .NET
pnpm test              # Node
pytest                 # Python
```

Details can be found in [README.md](README.md) and [docs/INDEX.md](docs/INDEX.md).
If you run into problems: file an issue ([Issues](https://github.com/mleem97/gregMod.CableTracer/issues)) or read [CONTRIBUTING.md](CONTRIBUTING.md).
