# Perf log tools

## Parse PerfScope logs

```powershell
pwsh ./tools/perf/perf-log-report.ps1 -LogPath "./bin/Release/logs/app-YYYYMMDD.log"
```

Outputs:
- `tools/perf/out/perfscope-raw.csv`
- `tools/perf/out/perfscope-summary.csv`
- `tools/perf/out/hotspots-top10.csv`

Use `hotspots-top10.csv` as input for `docs/performance/PERFORMANCE_EXECUTION_GUIDE.md` section "Hotspot analysis".
