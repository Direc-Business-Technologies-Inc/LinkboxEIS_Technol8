/**
 * @NApiVersion 2.1
 * @NScriptType Restlet
 */
define(['N/file'], function (file) {

    function doGet(context) {
        var fileId = context.fileId;

        if (!fileId) {
            return JSON.stringify({
                success: false,
                error: 'Missing required parameter: fileId'
            });
        }

        try {
            var f = file.load({ id: fileId });

            if (!isSupportedImage(f.fileType)) {
                return JSON.stringify({
                    success: false,
                    error: 'Unsupported file type',
                    fileType: f.fileType
                });
            }

            // IMPORTANT: already Base64 for images
            var response = {
                success: true,
                fileId: Number(fileId),
                name: f.name,
                mimeType: getMimeType(f.fileType),
                base64: f.getContents()
            };

            // 👇 explicitly return JSON STRING
            return JSON.stringify(response);

        } catch (e) {
            return JSON.stringify({
                success: false,
                error: e.name,
                message: e.message
            });
        }
    }

    function isSupportedImage(fileType) {
        return [
            'JPGIMAGE',
            'PNGIMAGE',
            'GIFIMAGE',
            'WEBPIMAGE'
        ].indexOf(fileType) !== -1;
    }

    function getMimeType(fileType) {
        switch (fileType) {
            case 'JPGIMAGE': return 'image/jpeg';
            case 'PNGIMAGE': return 'image/png';
            case 'GIFIMAGE': return 'image/gif';
            case 'WEBPIMAGE': return 'image/webp';
            default: return 'application/octet-stream';
        }
    }

    return {
        get: doGet
    };
});
