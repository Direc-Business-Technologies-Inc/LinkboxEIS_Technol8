/**
 * @NApiVersion 2.1
 * @NScriptType Restlet
 */
define(['N/search'], (nsSearch) => {

    const get = (request) => {
        try {
            const parentFolderName = request.parentFolderName; // e.g. "Shopify"
            const folderName       = request.folderName;       // e.g. SKU folder

            if (!parentFolderName || !folderName) {
                return JSON.stringify({
                    success: false,
                    message: 'parentFolderName and folderName are required'
                });
            }

            // 1️⃣ Find parent folder
            const parentFolderId = findFolderByName(parentFolderName);
            if (!parentFolderId) {
                return JSON.stringify({
                    success: false,
                    message: 'Parent folder not found'
                });
            }

            // 2️⃣ Find child folder under parent
            const folderId = findChildFolderByName(parentFolderId, folderName);
            if (!folderId) {
                return JSON.stringify({
                    success: false,
                    message: 'Folder not found under parent folder'
                });
            }

            // 3️⃣ Get latest UPDATED image
            const image = getLatestUpdatedImageInFolder(folderId);

            return JSON.stringify({
                success: true,
                parentFolder: parentFolderName,
                folder: folderName,
                image: image
            });

        } catch (e) {
            return JSON.stringify({
                success: false,
                error: e.name || 'UNEXPECTED_ERROR',
                message: e.message
            });
        }
    };

    /**
     * Find folder by exact name
     */
    const findFolderByName = (folderName) => {
        let folderId = null;

        nsSearch.create({
            type: 'folder',
            filters: [['name', 'is', folderName]],
            columns: ['internalid']
        }).run().each(r => {
            folderId = r.getValue('internalid');
            return false;
        });

        return folderId;
    };

    /**
     * Find child folder by name under a specific parent
     */
    const findChildFolderByName = (parentId, folderName) => {
        let folderId = null;

        nsSearch.create({
            type: 'folder',
            filters: [
                ['parent', 'anyof', parentId],
                'AND',
                ['name', 'is', folderName]
            ],
            columns: ['internalid']
        }).run().each(r => {
            folderId = r.getValue('internalid');
            return false;
        });

        return folderId;
    };

    /**
     * Get the LATEST UPDATED image inside a folder
     */
  const getLatestUpdatedImageInFolder = (folderId) => {
    let latestImage = null;

    nsSearch.create({
        type: 'file',
        filters: [
            ['folder', 'anyof', folderId],
            'AND',
            ['filetype', 'anyof', [
                'JPGIMAGE',
                'PNGIMAGE',
                'GIFIMAGE',
                'BMPIMAGE'
            ]]
        ],
        columns: [
            'internalid',
            'name',
            'filetype',
            nsSearch.createColumn({
                name: 'modified',            // ✅ correct column
                sort: nsSearch.Sort.DESC
            })
        ]
    }).run().each(r => {
        latestImage = {
            id: r.getValue('internalid'),
            name: r.getValue('name'),
            type: r.getValue('filetype'),
            lastModified: r.getValue('modified')
        };
        return false; // stop after latest
    });

    return latestImage;
};

    return { get };
});
