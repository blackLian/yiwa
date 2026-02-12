$ErrorActionPreference = 'Stop'

Write-Host "[P3 Smoke] Start" -ForegroundColor Cyan

$checks = @(
    @{ Name = 'validate_behavior'; Command = 'python'; Args = @('tests/validate_behavior.py') },
    @{ Name = 'validate_p3_progress'; Command = 'python'; Args = @('tests/validate_p3_progress.py') },
    @{ Name = 'validate_desktop_runtime_progress'; Command = 'python'; Args = @('tests/validate_desktop_runtime_progress.py') },
    @{ Name = 'validate_runtime_operator_progress'; Command = 'python'; Args = @('tests/validate_runtime_operator_progress.py') }
)

$results = @()
foreach ($check in $checks) {
    Write-Host "[P3 Smoke] Running $($check.Name)..." -ForegroundColor Yellow
    try {
        & $check.Command @($check.Args)
        $results += [PSCustomObject]@{ Name = $check.Name; Passed = $true }
    }
    catch {
        Write-Host "[P3 Smoke] FAILED: $($check.Name) -> $($_.Exception.Message)" -ForegroundColor Red
        $results += [PSCustomObject]@{ Name = $check.Name; Passed = $false }
    }
}

$failed = $results | Where-Object { -not $_.Passed }
if ($failed.Count -gt 0) {
    Write-Host "[P3 Smoke] Summary: $($failed.Count) checks failed" -ForegroundColor Red
    $results | Format-Table -AutoSize
    exit 1
}

Write-Host "[P3 Smoke] Summary: all checks passed" -ForegroundColor Green
$results | Format-Table -AutoSize
