/**
 * @NApiVersion 2.1
 * @NScriptType Restlet
 */
define(['N/search'], function (search) {

    var PAGE_SIZE = 10;

    function doGet(context) {
        try {
            var folderName = context.folder;

            if (!folderName) {
                var response = {
                    success: false,
                    error: 'Missing folder parameter'
                };
                return JSON.stringify(response);
            }

            var folderId = getFolderIdByName(folderName);
            if (!folderId) {
                var response = {
                    success: false,
                    error: 'Folder not found'
                };
                return JSON.stringify(response);
            }

            var result = getFirstPageOfImages(folderId);

            var response = {
                success: true,
                folder: folderName,
                folderId: folderId,
                pageSize: PAGE_SIZE,
                count: result.files.length,
                hasMore: result.hasMore,
                files: result.files
            };

            return JSON.stringify(response);

        } catch (e) {
            var response = {
                success: false,
                error: e.name,
                message: e.message
            };
            return JSON.stringify(response);
        }
    }

    function getFolderIdByName(folderName) {
        var res = search.create({
            type: search.Type.FOLDER,
            filters: [
                ['name', 'is', folderName],
                'AND',
                ['isinactive', 'is', 'F']
            ],
            columns: ['internalid']
        }).run().getRange({ start: 0, end: 1 });

        return res.length ? res[0].getValue('internalid') : null;
    }

    function getFirstPageOfImages(folderId) {
        var fileSearch = search.create({
            type: 'file',   // IMPORTANT: do not use search.Type.FILE
            filters: [
                ['folder', 'is', folderId]
            ],
            columns: [
    'internalid',
    'name',
    'modified'
]
        });

        var files = [];
        var hasMore = false;

        fileSearch.run().each(function (r) {

            if (files.length >= PAGE_SIZE) {
                hasMore = true;
                return false;
            }

            var name = (r.getValue('name') || '').toLowerCase();

            if (
                name.endsWith('.jpg') ||
                name.endsWith('.jpeg') ||
                name.endsWith('.png')
            ) {
                files.push({
                    fileId: Number(r.getValue('internalid')),
                    name: r.getValue('name'),
                    lastModified: r.getValue('modified')
                });
            }

            return true;
        });

        return {
            files: files,
            hasMore: hasMore
        };
    }

    return {
        get: doGet,
        post: doGet
    };
});
