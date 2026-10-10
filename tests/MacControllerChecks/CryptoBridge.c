/* Test-only OpenSSL adapter, not an application dependency. */
#include <openssl/evp.h>
#include <openssl/hmac.h>
#include <openssl/crypto.h>
#include <stdint.h>
int fixture_hmac(const uint8_t *key, int length, const uint8_t *data, int count, uint8_t *out) {
    unsigned int size = 0;
    return HMAC(EVP_sha256(),key,length,data,count,out,&size) != NULL && size == 32;
}
int fixture_equal(const uint8_t *a, const uint8_t *b, int length) { return CRYPTO_memcmp(a,b,length) == 0; }
int fixture_gcm(int encrypt, const uint8_t *key, const uint8_t *nonce, const uint8_t *aad, int aadSize,
                const uint8_t *data, int size, uint8_t *out, uint8_t *tag) {
    EVP_CIPHER_CTX *ctx = EVP_CIPHER_CTX_new(); if (!ctx) return 0;
    int ok = 0, count = 0, final = 0;
    if (EVP_CipherInit_ex(ctx,EVP_aes_256_gcm(),NULL,key,nonce,encrypt) != 1) goto done;
    if (aadSize && EVP_CipherUpdate(ctx,NULL,&count,aad,aadSize) != 1) goto done;
    if (EVP_CipherUpdate(ctx,out,&count,data,size) != 1) goto done;
    if (!encrypt && EVP_CIPHER_CTX_ctrl(ctx,EVP_CTRL_GCM_SET_TAG,16,tag) != 1) goto done;
    if (EVP_CipherFinal_ex(ctx,out+count,&final) != 1) goto done;
    if (encrypt && EVP_CIPHER_CTX_ctrl(ctx,EVP_CTRL_GCM_GET_TAG,16,tag) != 1) goto done;
    ok = count + final == size;
done: EVP_CIPHER_CTX_free(ctx); return ok;
}
