/**
 * Utilidad de pre-hasheo de contraseñas en el frontend (SHA-256).
 * Evita que la contraseña viaje en texto plano en el cuerpo de la petición HTTP,
 * complementando el hasheo PBKDF2 + sal aleatoria en el backend (defensa en profundidad).
 */
(function (global) {
    "use strict";

    /**
     * Fallback puro en JavaScript para SHA-256 cuando crypto.subtle no está disponible
     * (por ejemplo, en contextos HTTP no-localhost).
     */
    function sha256Fallback(ascii) {
        function rightRotate(value, amount) {
            return (value >>> amount) | (value << (32 - amount));
        }

        var mathPow = Math.pow;
        var maxWord = mathPow(2, 32);
        var lengthProperty = "length";
        var i, j;
        var result = "";

        var words = [];
        var asciiBitLength = ascii[lengthProperty] * 8;

        var hash = sha256Fallback.h = sha256Fallback.h || [];
        var k = sha256Fallback.k = sha256Fallback.k || [];
        var primeCounter = k[lengthProperty];

        var isComposite = {};
        for (var candidate = 2; primeCounter < 64; candidate++) {
            if (!isComposite[candidate]) {
                for (i = 0; i < 313; i += candidate) {
                    isComposite[i] = candidate;
                }
                hash[primeCounter] = (mathPow(candidate, 0.5) * maxWord) | 0;
                k[primeCounter++] = (mathPow(candidate, 1 / 3) * maxWord) | 0;
            }
        }

        ascii += "\x80";
        while (ascii[lengthProperty] % 64 - 56) ascii += "\x00";
        for (i = 0; i < ascii[lengthProperty]; i++) {
            j = ascii.charCodeAt(i);
            if (j >> 8) return "";
            words[i >> 2] |= j << ((3 - i) % 4) * 8;
        }
        words[words[lengthProperty]] = ((asciiBitLength / maxWord) | 0);
        words[words[lengthProperty]] = (asciiBitLength);

        for (j = 0; j < words[lengthProperty];) {
            var w = words.slice(j, j += 16);
            var oldHash = hash;
            hash = hash.slice(0, 8);

            for (i = 0; i < 64; i++) {
                var w15 = w[i - 15], w2 = w[i - 2];
                var a = hash[0], e = hash[4];
                var temp1 = hash[7]
                    + (rightRotate(e, 6) ^ rightRotate(e, 11) ^ rightRotate(e, 25))
                    + ((e & hash[5]) ^ ((~e) & hash[6]))
                    + k[i]
                    + (w[i] = (i < 16) ? w[i] : (
                        w[i - 16]
                        + (rightRotate(w15, 7) ^ rightRotate(w15, 18) ^ (w15 >>> 3))
                        + w[i - 7]
                        + (rightRotate(w2, 17) ^ rightRotate(w2, 19) ^ (w2 >>> 10))
                    ) | 0);
                var temp2 = (rightRotate(a, 2) ^ rightRotate(a, 13) ^ rightRotate(a, 22))
                    + ((a & hash[1]) ^ (a & hash[2]) ^ (hash[1] & hash[2]));

                hash = [(temp1 + temp2) | 0].concat(hash);
                hash[4] = (hash[4] + temp1) | 0;
            }

            for (i = 0; i < 8; i++) {
                hash[i] = (hash[i] + oldHash[i]) | 0;
            }
        }

        for (i = 0; i < 8; i++) {
            for (j = 3; j + 1; j--) {
                var b = (hash[i] >> (j * 8)) & 255;
                result += ((b < 16) ? 0 : "") + b.toString(16);
            }
        }
        return result;
    }

    /**
     * Calcula el hash SHA-256 en formato hexadecimal minúscula (64 caracteres).
     */
    async function sha256Hex(text) {
        if (global.crypto && global.crypto.subtle && typeof TextEncoder !== "undefined") {
            const encoder = new TextEncoder();
            const data = encoder.encode(text);
            const hashBuffer = await global.crypto.subtle.digest("SHA-256", data);
            const hashArray = Array.from(new Uint8Array(hashBuffer));
            return hashArray.map(function (b) {
                return b.toString(16).padStart(2, "0");
            }).join("");
        }

        // Codificar UTF-8 básico para el fallback
        var utf8 = unescape(encodeURIComponent(text));
        return sha256Fallback(utf8);
    }

    /**
     * Valida las reglas OWASP de complejidad de contraseña en el cliente antes de hashear.
     * Retorna null si es válida, o un mensaje de error descriptivo.
     */
    function validateOwaspPassword(password, dni, celular) {
        if (!password || password.length < 8) {
            return "La contraseña debe tener al menos 8 caracteres.";
        }
        if (password.length > 100) {
            return "La contraseña no debe superar los 100 caracteres.";
        }
        if (!/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$/.test(password)) {
            return "La contraseña debe incluir mayúsculas, minúsculas, números y al menos un símbolo.";
        }

        var lower = password.toLowerCase();
        var trivialSequences = ["123456", "234567", "345678", "456789", "567890", "abcdef", "qwerty", "password", "contrasena"];
        for (var i = 0; i < trivialSequences.length; i++) {
            if (lower.indexOf(trivialSequences[i]) !== -1) {
                return "La contraseña no debe contener secuencias simples ni palabras comunes.";
            }
        }

        if (dni && dni.length >= 6 && password.indexOf(dni) !== -1) {
            return "La contraseña no puede incluir tu número de DNI.";
        }
        if (celular && celular.length >= 6 && password.indexOf(celular) !== -1) {
            return "La contraseña no puede incluir tu número de celular.";
        }

        return null;
    }

    /**
     * Enlaza el pre-hasheo SHA-256 al evento submit de un formulario.
     * @param {Object} config
     * @param {string} config.formId - ID del formulario.
     * @param {string[]} config.passwordInputIds - IDs de los inputs de contraseña a hashear.
     * @param {string} [config.isHashedInputId] - ID del input oculto IsClientHashed.
     * @param {boolean} [config.validateOwasp] - Si debe validar reglas OWASP antes de hashear.
     * @param {string} [config.errorElementId] - ID del span de error para la contraseña.
     * @param {string} [config.confirmInputId] - ID del input de confirmación.
     * @param {string} [config.confirmErrorId] - ID del span de error de confirmación.
     * @param {string} [config.dniInputId] - ID del input DNI.
     * @param {string} [config.celularInputId] - ID del input Celular.
     */
    function attachPasswordHashing(config) {
        var form = document.getElementById(config.formId);
        if (!form) return;

        var isSubmitting = false;

        form.addEventListener("submit", async function (e) {
            if (isSubmitting) return;

            var inputs = (config.passwordInputIds || [])
                .map(function (id) { return document.getElementById(id); })
                .filter(function (el) { return el !== null; });

            if (inputs.length === 0) return;

            var primaryInput = inputs[0];
            var primaryValue = primaryInput.value;

            // Si el campo está vacío (ej. Login vacío o Profile sin cambio de contraseña), dejar flujo normal
            if (!primaryValue) return;

            e.preventDefault();

            if (config.minLength && primaryValue.length < config.minLength) {
                var minErrEl = config.errorElementId ? document.getElementById(config.errorElementId) : null;
                var minMsg = "La contraseña debe tener al menos " + config.minLength + " caracteres";
                if (minErrEl) {
                    minErrEl.textContent = minMsg;
                } else {
                    alert(minMsg);
                }
                primaryInput.focus();
                return;
            }

            if (config.confirmInputId) {
                var confirmEl = document.getElementById(config.confirmInputId);
                var confirmErrEl = config.confirmErrorId ? document.getElementById(config.confirmErrorId) : null;
                if (confirmEl && confirmEl.value !== primaryValue) {
                    var matchMsg = "Las contraseñas no coinciden";
                    if (confirmErrEl) {
                        confirmErrEl.textContent = matchMsg;
                    } else {
                        alert(matchMsg);
                    }
                    confirmEl.focus();
                    return;
                } else if (confirmErrEl) {
                    confirmErrEl.textContent = "";
                }
            }

            if (config.validateOwasp) {
                var dniVal = config.dniInputId ? (document.getElementById(config.dniInputId)?.value || "") : "";
                var celVal = config.celularInputId ? (document.getElementById(config.celularInputId)?.value || "") : "";
                var errorMsg = validateOwaspPassword(primaryValue, dniVal, celVal);
                var errorEl = config.errorElementId ? document.getElementById(config.errorElementId) : null;

                if (errorMsg) {
                    if (errorEl) errorEl.textContent = errorMsg;
                    primaryInput.focus();
                    return;
                } else if (errorEl) {
                    errorEl.textContent = "";
                }
            }

            // El campo visible NO se modifica: se envía un input oculto con el hash
            // y se quita el atributo "name" del campo visible para que no viaje en texto plano.
            for (var i = 0; i < inputs.length; i++) {
                var visible = inputs[i];
                if (visible.value && visible.name) {
                    var hidden = document.createElement("input");
                    hidden.type = "hidden";
                    hidden.name = visible.name;
                    hidden.value = await sha256Hex(visible.value);
                    visible.removeAttribute("name");
                    form.appendChild(hidden);
                }
            }

            if (config.isHashedInputId) {
                var hiddenFlag = document.getElementById(config.isHashedInputId);
                if (hiddenFlag) {
                    hiddenFlag.value = "true";
                }
            }

            isSubmitting = true;
            form.submit();
        });
    }

    global.DfitPasswordHash = {
        sha256Hex: sha256Hex,
        validateOwaspPassword: validateOwaspPassword,
        attachPasswordHashing: attachPasswordHashing
    };
})(window);
