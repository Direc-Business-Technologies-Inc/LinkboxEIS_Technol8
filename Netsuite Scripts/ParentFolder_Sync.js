/**
 * @NApiVersion 2.1
 * @NScriptType Restlet
 * @NModuleScope SameAccount
 *
 * RESTlet: Shopify folder inventory (1st-level only)
 * Stores the response object in a variable, then returns JSON.stringify(variable)
 */

define(['N/search', 'N/log'], (search, log) => {
  const IMAGE_FILETYPES = ['JPGIMAGE', 'JPEGIMAGE', 'PNGIMAGE'];

  const asInt = (v) => {
    const n = parseInt(v, 10);
    return Number.isFinite(n) ? n : null;
  };

  function findFolderIdByName(folderName) {
    const s = search.create({
      type: search.Type.FOLDER,
      filters: [['name', 'is', folderName]],
      columns: ['internalid']
    });

    const r = s.run().getRange({ start: 0, end: 1 });
    return (r && r.length) ? asInt(r[0].getValue({ name: 'internalid' })) : null;
  }

  function getChildFolders(parentFolderId) {
    const out = [];

    const s = search.create({
      type: search.Type.FOLDER,
      filters: [['parent', 'anyof', String(parentFolderId)]],
      columns: ['internalid', 'name']
    });

    const paged = s.runPaged({ pageSize: 1000 });
    paged.pageRanges.forEach((pr) => {
      const page = paged.fetch({ index: pr.index });
      page.data.forEach((row) => {
        out.push({
          folderId: asInt(row.getValue({ name: 'internalid' })),
          folderName: row.getValue({ name: 'name' })
        });
      });
    });

    out.sort((a, b) => String(a.folderName).localeCompare(String(b.folderName)));
    return out;
  }

  function getImageFileIdsInFolders(folderIds) {
    if (!folderIds || !folderIds.length) return [];

    const folderIdStrings = folderIds.map(String);

    const out = [];
    const s = search.create({
      type: search.Type.FILE,
      filters: [
        ['folder', 'anyof', folderIdStrings], 'AND',
        ['filetype', 'anyof', IMAGE_FILETYPES]
      ],
      columns: ['internalid', 'folder']
    });

    const paged = s.runPaged({ pageSize: 1000 });
    paged.pageRanges.forEach((pr) => {
      const page = paged.fetch({ index: pr.index });
      page.data.forEach((row) => {
        out.push({
          fileId: asInt(row.getValue({ name: 'internalid' })),
          folderId: asInt(row.getValue({ name: 'folder' }))
        });
      });
    });

    return out;
  }

  function buildPayload(parentFolderName, parentFolderId, childFolders, fileRows) {
    const byFolder = {};

    childFolders.forEach((f) => {
      byFolder[f.folderId] = {
        folderId: f.folderId,
        folderName: f.folderName,
        files: []
      };
    });

    fileRows.forEach((r) => {
      const bucket = byFolder[r.folderId];
      if (bucket && r.fileId) bucket.files.push(r.fileId);
    });

    return {
      ok: true,
      parentFolder: { folderId: parentFolderId, folderName: parentFolderName },
      folders: Object.values(byFolder)
    };
  }

  function handle(request) {
    const parentFolderName =
      (request && request.parentFolderName) ? String(request.parentFolderName) : 'Shopify';

    const parentFolderId = findFolderIdByName(parentFolderName);
    if (!parentFolderId) {
      // store error payload then stringify
      const errorPayload = { ok: false, error: `Parent folder not found: ${parentFolderName}` };
      return JSON.stringify(errorPayload);
    }

    const childFolders = getChildFolders(parentFolderId);
    const childFolderIds = childFolders.map((x) => x.folderId).filter(Boolean);

    const fileRows = getImageFileIdsInFolders(childFolderIds);

    // ✅ store to a separate variable first
    const responsePayload = buildPayload(parentFolderName, parentFolderId, childFolders, fileRows);

    // ✅ then return JSON.stringify(variable)
    return JSON.stringify(responsePayload);
  }

  function get(request) {
    try {
      return handle(request);
    } catch (e) {
      log.error({ title: 'RESTlet GET error', details: e });

      const errorPayload = { ok: false, error: String(e && e.message ? e.message : e) };
      return JSON.stringify(errorPayload);
    }
  }

  function post(request) {
    try {
      return handle(request || {});
    } catch (e) {
      log.error({ title: 'RESTlet POST error', details: e });

      const errorPayload = { ok: false, error: String(e && e.message ? e.message : e) };
      return JSON.stringify(errorPayload);
    }
  }

  return { get, post };
});
