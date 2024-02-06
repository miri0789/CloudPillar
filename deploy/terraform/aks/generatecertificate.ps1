
# Generate private key
openssl ecparam -name secp384r1 -genkey -noout -out private.key

# Generate certificate signing request (CSR)
openssl req -new -key private.key -out csr.csr -subj "/CN=devbe.cloudpillar.net"

# Generate self-signed certificate
openssl x509 -req -days 36135 -in csr.csr -signkey private.key -out certificate.crt

# Convert private key and certificate to PFX format
openssl pkcs12 -export -out certificate.pfx -inkey private.key -in certificate.crt -passout pass:

# Clean up temporary files
Remove-Item -Path private.key, csr.csr, certificate.crt -Force
