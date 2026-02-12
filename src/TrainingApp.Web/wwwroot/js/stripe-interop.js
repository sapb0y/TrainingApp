// Stripe Elements interop for Blazor registration
window.stripeInterop = {
    stripe: null,
    elements: null,
    cardElement: null,

    init: function (publishableKey) {
        this.stripe = Stripe(publishableKey);
        this.elements = this.stripe.elements();
        this.cardElement = this.elements.create('card', {
            style: {
                base: {
                    color: '#E0E0E0',
                    fontFamily: 'Inter, sans-serif',
                    fontSize: '16px',
                    '::placeholder': { color: '#666' }
                },
                invalid: { color: '#EF4444' }
            }
        });
    },

    mountCard: function (elementId) {
        if (this.cardElement) {
            this.cardElement.mount('#' + elementId);
        }
    },

    confirmSetupIntent: async function (clientSecret) {
        if (!this.stripe || !this.cardElement) {
            return { error: 'Stripe not initialized' };
        }

        var result = await this.stripe.confirmCardSetup(clientSecret, {
            payment_method: { card: this.cardElement }
        });

        if (result.error) {
            return { error: result.error.message };
        }

        return { paymentMethodId: result.setupIntent.payment_method };
    },

    destroy: function () {
        if (this.cardElement) {
            this.cardElement.destroy();
            this.cardElement = null;
        }
        this.elements = null;
        this.stripe = null;
    }
};
