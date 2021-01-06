FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime

COPY certificates /usr/share/ca-certificates/
RUN echo "swiftcom_root.crt" >> /etc/ca-certificates.conf && \
    echo "swiftcom_rabbit_root.cer" >> /etc/ca-certificates.conf && \
    echo "epayments_rabbit_root.cer" >> /etc/ca-certificates.conf && \
    echo "epayments_root.crt" >> /etc/ca-certificates.conf && \
    update-ca-certificates && \
    sed 's/DEFAULT@SECLEVEL=2/DEFAULT@SECLEVEL=1/' /etc/ssl/openssl.cnf > /etc/ssl/openssl.cnf.changed && mv /etc/ssl/openssl.cnf.changed /etc/ssl/openssl.cnf

WORKDIR /app
COPY publish .

ENTRYPOINT ["dotnet", "ePayments.ClientService.Verification.Api.dll"]