/**
 * @NApiVersion 2.1
 * @NScriptType Restlet
 */
define(['N/record', 'N/log'], (record, log) => {

    const get = (params) => {

        const itemId    = params.itemId;
        const ecomId    = params.ecomId;
        const product   = params.productId;
        const variant   = params.variantId;   // OPTIONAL
        const basePrice = params.basePrice;   // OPTIONAL ✅

        // 🔴 HARD BLOCKING (optional params excluded)
        if (!ecomId || !product || !itemId) {
            throw new Error(
                `BLOCKED: Missing required parameters.
                 ecomId=${ecomId}, product=${product}, itemId=${itemId}`
            );
        }

        const platformRecord = record.create({
            type: 'customrecord_dbti_platform',
            isDynamic: false
        });

        platformRecord.setValue({
            fieldId: 'custrecord_dbti_ecom_item',
            value: Number(itemId)
        });

        platformRecord.setValue({
            fieldId: 'custrecord_dbti_ecom_customer',
            value: Number(ecomId)
        });

        platformRecord.setValue({
            fieldId: 'custrecord_dbti_ecom_platid',
            value: product
        });

        // OPTIONAL: Variant ID
        if (variant) {
            platformRecord.setValue({
                fieldId: 'custrecord_dbti_ecom_skuid',
                value: variant
            });
        }

        // OPTIONAL: Base Price
        if (basePrice) {
            platformRecord.setValue({
                fieldId: 'custrecord_dbti_ecom_baseprice', // <-- confirm field ID
                value: Number(basePrice)
            });
        }

        const recordId = platformRecord.save();

        log.audit({
            title: 'Platform Record Created',
            details: { recordId, itemId, ecomId, product, variant, basePrice }
        });

        const response = {
            success: true,
            recordId: recordId,
            itemId: itemId,
            ecomId: ecomId,
            productId: product,
            variantId: variant || null,
            basePrice: basePrice || null
        };

        return JSON.stringify(response);
    };

    return { get };
});
