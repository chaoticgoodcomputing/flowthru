/**
 * Zero-GCP alternative to Program.cs — see SPIKE.md.
 *
 * Verifies the same thing (can the Sheets API create + read a native Table
 * with typed columns) without a GCP project, using Apps Script's advanced
 * Sheets service. Apps Script supplies the project transparently; you only
 * approve the in-editor authorization prompt on first run.
 *
 * Setup:
 *   1. Open YOUR Google Sheet -> Extensions -> Apps Script.
 *   2. Paste this file in.
 *   3. In the editor, Services (+) -> add "Sheets API" (the advanced service).
 *   4. Run spikeTables(), approve the auth prompt, then View -> Logs.
 */
function spikeTables() {
  const ssId = SpreadsheetApp.getActiveSpreadsheet().getId();

  // Clean slate: drop our tab if a prior run left it (drops its table too).
  const meta = Sheets.Spreadsheets.get(ssId, { fields: 'sheets(properties(sheetId,title))' });
  const stale = meta.sheets.find(s => s.properties.title === 'FlowthruTablesSpike');
  if (stale) {
    Sheets.Spreadsheets.batchUpdate(
      { requests: [{ deleteSheet: { sheetId: stale.properties.sheetId } }] }, ssId);
  }
  const added = Sheets.Spreadsheets.batchUpdate(
    { requests: [{ addSheet: { properties: { title: 'FlowthruTablesSpike' } } }] }, ssId);
  const sheetId = added.replies[0].addSheet.properties.sheetId;
  Logger.log('[1/4] Auth + tab OK (sheetId ' + sheetId + ')');

  // Seed header + two data rows.
  Sheets.Spreadsheets.Values.update(
    { values: [
        ['Name', 'Amount', 'When'],
        ['alpha', 12.5, '2026-01-15 09:30:00'],
        ['beta', 7, '2026-02-01 14:00:00'],
    ] },
    ssId, 'FlowthruTablesSpike!A1:C3', { valueInputOption: 'USER_ENTERED' });
  Logger.log('[2/4] Wrote header + 2 rows');

  // AddTable with typed columns.
  try {
    const res = Sheets.Spreadsheets.batchUpdate({ requests: [{ addTable: { table: {
      name: 'FlowthruSpikeTable',
      range: { sheetId: sheetId, startRowIndex: 0, endRowIndex: 3, startColumnIndex: 0, endColumnIndex: 3 },
      columnProperties: [
        { columnIndex: 0, columnName: 'Name', columnType: 'TEXT' },
        { columnIndex: 1, columnName: 'Amount', columnType: 'DOUBLE' },
        { columnIndex: 2, columnName: 'When', columnType: 'DATE_TIME' },
      ],
    } } }] }, ssId);
    Logger.log('[3/4] AddTable OK -> ' + res.replies[0].addTable.table.name);
  } catch (e) {
    Logger.log('[3/4] AddTable FAILED: ' + e.message);
    Logger.log('FAIL: could not create a Table via the API. See SPIKE.md > failure.');
    return;
  }

  // Read the table metadata + column types back.
  const after = Sheets.Spreadsheets.get(ssId, { fields: 'sheets(properties(sheetId),tables)' });
  const tab = after.sheets.find(s => s.properties.sheetId === sheetId);
  const table = (tab.tables || []).find(t => t.name === 'FlowthruSpikeTable');
  if (!table) {
    Logger.log('[4/4] FAIL: table not present in Sheet.tables after creation.');
    return;
  }
  Logger.log('[4/4] Read-back columns:');
  table.columnProperties
    .sort((a, b) => a.columnIndex - b.columnIndex)
    .forEach(c => Logger.log('       col ' + c.columnIndex + '  ' + c.columnName + '  ' + c.columnType));
  Logger.log('PASS: created + read a typed Table; column types round-trip.');
  Logger.log('(Report the exact columnType strings above.)');
}
