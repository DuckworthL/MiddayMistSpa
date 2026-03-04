$ErrorActionPreference = "Stop"
$base = "http://localhost:5286/api"

# Login
$login = Invoke-RestMethod -Uri "$base/auth/login" -Method Post -ContentType "application/json" -Body '{"username":"superadmin","password":"SuperAdmin@2026!"}'
$h = @{ Authorization = "Bearer $($login.token.accessToken)" }
Write-Host "AUTH OK (User: $($login.user.username))" -ForegroundColor Green

# ============================================================
# STEP 11: Use Confirmed appointment (Id=1) for workflow test
# ============================================================
Write-Host "`n=== STEP 11: Using Confirmed Appointment Id=1 ===" -ForegroundColor Cyan
$apptId = 1

# ============================================================
# STEP 12: Appointment workflow: Check-In -> Start -> Complete (already Confirmed)
# ============================================================
Write-Host "`n=== STEP 12: Appointment Status Workflow ===" -ForegroundColor Cyan

# Check-In (note: route is /check-in not /checkin)
try {
    $r = Invoke-RestMethod -Uri "$base/appointments/$apptId/check-in" -Method Post -Headers $h -ContentType "application/json"
    Write-Host "  Check-In: $($r.status) - PASS" -ForegroundColor Green
} catch {
    try { $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream()); Write-Host "  Check-In FAIL: $($sr.ReadToEnd())" -ForegroundColor Red } catch { Write-Host "  Check-In FAIL: $($_.Exception.Message)" -ForegroundColor Red }
}

# Start (In Progress)
try {
    $r = Invoke-RestMethod -Uri "$base/appointments/$apptId/start" -Method Post -Headers $h -ContentType "application/json"
    Write-Host "  Start: $($r.status) - PASS" -ForegroundColor Green
} catch {
    try { $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream()); Write-Host "  Start FAIL: $($sr.ReadToEnd())" -ForegroundColor Red } catch { Write-Host "  Start FAIL: $($_.Exception.Message)" -ForegroundColor Red }
}

# Complete
try {
    $r = Invoke-RestMethod -Uri "$base/appointments/$apptId/complete" -Method Post -Headers $h -ContentType "application/json"
    Write-Host "  Complete: $($r.status) - PASS" -ForegroundColor Green
} catch {
    try { $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream()); Write-Host "  Complete FAIL: $($sr.ReadToEnd())" -ForegroundColor Red } catch { Write-Host "  Complete FAIL: $($_.Exception.Message)" -ForegroundColor Red }
}

# ============================================================
# STEP 13: Test delete guard - should FAIL for Completed appointment
# ============================================================
Write-Host "`n=== STEP 13: Delete Guard (Completed Id=1 - should fail) ===" -ForegroundColor Cyan
try {
    Invoke-RestMethod -Uri "$base/appointments/$apptId" -Method Delete -Headers $h
    Write-Host "  UNEXPECTED: Delete succeeded (should have been blocked!)" -ForegroundColor Red
} catch {
    Write-Host "  EXPECTED BLOCK: Cannot delete completed appointment - PASS" -ForegroundColor Green
}

# ============================================================
# STEP 14: Delete Guard - Delete a Cancelled appointment (Id=2)
# ============================================================
Write-Host "`n=== STEP 14: Delete Guard (Cancelled Id=2 - should succeed) ===" -ForegroundColor Cyan
try {
    Invoke-RestMethod -Uri "$base/appointments/2" -Method Delete -Headers $h
    Write-Host "  DELETE OK - Cancelled appointment deleted successfully - PASS" -ForegroundColor Green
} catch {
    try { $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream()); Write-Host "  FAIL: $($sr.ReadToEnd())" -ForegroundColor Red } catch { Write-Host "  FAIL: $($_.Exception.Message)" -ForegroundColor Red }
}

# ============================================================
# STEP 15: Test double-void protection
# ============================================================
Write-Host "`n=== STEP 15: Double Void Protection ===" -ForegroundColor Cyan
$voidBody = @{ reason = "test double void" } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$base/transactions/9/void" -Method Post -Headers $h -ContentType "application/json" -Body $voidBody
    Write-Host "  UNEXPECTED: Double void succeeded!" -ForegroundColor Red
} catch {
    Write-Host "  EXPECTED BLOCK: Cannot void already-voided transaction" -ForegroundColor Green
}

# ============================================================
# STEP 16: Test Payment on already-paid transaction
# ============================================================
Write-Host "`n=== STEP 16: Double Payment Protection ===" -ForegroundColor Cyan
$payBody = @{ paymentMethod = "Cash"; amountTendered = 1000 } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$base/transactions/10/payment" -Method Post -Headers $h -ContentType "application/json" -Body $payBody
    Write-Host "  UNEXPECTED: Double payment succeeded!" -ForegroundColor Red
} catch {
    Write-Host "  EXPECTED BLOCK: Cannot pay already-paid transaction" -ForegroundColor Green
}

# ============================================================
# STEP 17: Verify transaction list with filters
# ============================================================
Write-Host "`n=== STEP 17: Transaction List with Filters ===" -ForegroundColor Cyan
$txList = Invoke-RestMethod -Uri "$base/transactions?pageSize=5" -Headers $h
Write-Host "  Total transactions: $($txList.totalCount)"
foreach ($t in $txList.items) {
    Write-Host "  TxId:$($t.transactionId) Num:$($t.transactionNumber) Status:$($t.paymentStatus) Total:$($t.totalAmount)" 
}

Write-Host "`n=== ALL TESTS COMPLETE ===" -ForegroundColor Green
