/**
 * @NApiVersion 2.1
 * @NScriptType Restlet
 */
define(['N/log'], (log) => {

    /**
     * GET handler
     * Example:
     * /app/site/hosting/restlet.nl?script=XXX&deploy=1&code=ABC123&shopId=SHOP01
     */
    const get = (requestParams) => {
        const code = requestParams.code || null;
        const shopId = requestParams.shopId || null;

        log.debug('GET Params', { code, shopId });

        if (!code || !shopId) {
            return {
                success: false,
                message: 'Missing required parameters: code or shopId'
            };
        }

        return {
            success: true,
            method: 'GET',
            code,
            shopId
        };
    };

    /**
     * POST handler
     * Example body:
     * {
     *   "code": "ABC123",
     *   "shopId": "SHOP01"
     * }
     */
    const post = (requestBody) => {
        const code = requestBody.code || null;
        const shopId = requestBody.shopId || null;

        log.debug('POST Body', { code, shopId });

        if (!code || !shopId) {
            return {
                success: false,
                message: 'Missing required fields: code or shopId'
            };
        }

        return {
            success: true,
            method: 'POST',
            code,
            shopId
        };
    };

    return {
        get,
        post
    };
});
