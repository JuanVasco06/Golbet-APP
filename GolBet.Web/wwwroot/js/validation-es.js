// Same decimal grammar as the server binder; no thousands separators.
(function ($) {
    const decimal = /^\d+([.,]\d{1,2})?$/;
    const parse = value => Number(String(value).replace(",", "."));
    const originalRange = $.validator.methods.range;
    $.validator.methods.number = function (value, element) {
        return this.optional(element) || decimal.test(value.trim());
    };
    $.validator.methods.range = function (value, element, params) {
        if (element.tagName === "SELECT") return originalRange.call(this, value, element, params);
        return this.optional(element) || (decimal.test(value.trim()) && parse(value) >= parse(params[0]) && parse(value) <= parse(params[1]));
    };
    $.extend($.validator.messages, { required: "Este campo es obligatorio.", number: "Ingrese un número con máximo dos decimales.", url: "Ingrese una URL válida.", date: "Ingrese una fecha válida." });
})(jQuery);
