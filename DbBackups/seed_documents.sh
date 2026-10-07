#!/bin/bash
# Тестовые PDF для страницы декана. Запускать после seed_data_updated.sql:
#   API_URL=http://localhost:8001 ./DbBackups/seed_documents.sh

set -u

API_URL="${API_URL:-http://localhost:8001}"
REPORT_IDS="${REPORT_IDS:-1 2 3 4 5 6 7 8 9 10 11}"
SEMESTER_IDS="${SEMESTER_IDS:-1 2 3}"

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT

make_pdf() {
    local path="$1" text="$2"
    local stream="BT /F1 18 Tf 72 720 Td (${text}) Tj ET"
    {
        printf '%%PDF-1.4\n'
        printf '1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n'
        printf '2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj\n'
        printf '3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >> endobj\n'
        printf '4 0 obj << /Length %d >> stream\n%s\nendstream endobj\n' "${#stream}" "$stream"
        printf '5 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n'
        printf 'trailer << /Root 1 0 R >>\n%%%%EOF\n'
    } > "$path"
}

call() {
    local label="$1"; shift
    local code
    code=$(curl -s -o "$TMP_DIR/response" -w '%{http_code}' "$@")
    printf '%-40s HTTP %s  %s\n' "$label" "$code" "$(head -c 160 "$TMP_DIR/response")"
}

echo "== Отчёты преподавателей"
for id in $REPORT_IDS; do
    make_pdf "$TMP_DIR/report_$id.pdf" "Report $id"
    call "upload report $id" -X POST -F "file=@$TMP_DIR/report_$id.pdf;filename=Отчёт_ПОЗ_$id.pdf;type=application/pdf" \
        "$API_URL/ReportFile/Upload/$id"
done

echo "== Графики ПОЗ"
for semester in $SEMESTER_IDS; do
    make_pdf "$TMP_DIR/schedule_$semester.pdf" "Schedule semester $semester"
    call "upload schedule, semester $semester" -X POST -F "semesterId=$semester" \
        -F "file=@$TMP_DIR/schedule_$semester.pdf;filename=График_ПОЗ_семестр_$semester.pdf;type=application/pdf" \
        "$API_URL/ScheduleDocument/Upload"
done

echo "== Статусы"
call "approve report 1" -X POST "$API_URL/DeanDocuments/ApproveReport/1"
call "reject report 3" -X POST -H 'Content-Type: application/json' \
    -d '{"comment":"Нет подписи заведующего кафедрой"}' "$API_URL/DeanDocuments/RejectReport/3"

schedule_id() {
    curl -s "$API_URL/ScheduleDocument/GetCurrent?semesterId=$1" | sed -n 's/.*"id":\([0-9]*\).*/\1/p'
}
first=$(schedule_id "$(echo $SEMESTER_IDS | awk '{print $1}')")
second=$(schedule_id "$(echo $SEMESTER_IDS | awk '{print $2}')")
[ -n "$first" ] && call "approve schedule $first" -X POST "$API_URL/DeanDocuments/ApproveSchedule/$first"
[ -n "$second" ] && call "reject schedule $second" -X POST -H 'Content-Type: application/json' \
    -d '{"comment":"Даты пересекаются с сессией"}' "$API_URL/DeanDocuments/RejectSchedule/$second"

echo "Готово: $API_URL/DeanDocuments/GetReports и $API_URL/DeanDocuments/GetSchedules"
