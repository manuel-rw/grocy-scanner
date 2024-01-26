<div align="center">
    <h1>Grocy Scanner</h1>
    <h4>Lazy version of <a href="https://github.com/Forceu/barcodebuddy">Barcodebuddy</a></h4>
</div>

## About
Scan your products with a camera and add them to your Grocy in one click.
It attempts to find an existing product with the barcode or will search and create one automatically if Grocy does not know this product yet.
This only works with Products that have a barcode and are available on one of the providers.

This is heavily inspired from [barcodebuddy](https://github.com/Forceu/barcodebuddy).
However, instead of prompting you to create the product, it will create the product itself.

![](./Documentation/screenshot-scanner.png)
![](./Documentation/screenshot-purchase-product.png)

## Installation
### Docker Compose
```yaml
version: '3'
services:
  grocy-scanner:
    container_name: grocy-scanner
    image: ghcr.io/manuel-rw/grocy-scanner:latest
    restart: always
    environment:
      - "Grocy__BaseUrl=<YOUR-GROCY-HOST>"
      - "Grocy__ApiKey=<YOUR-API-KEY>"
    ports:
      - '7575:80'
```
Application will start on http://your-hostname:7575

### TrueNAS
- Add the TrueCharts catalog: https://truecharts.org/manual/intro
- Search for ``Custom App``in the catalog (not the TrueNAS custom app)
- Click on Install
- Enter ``ghcr.io/manuel-rw/grocy-scanner`` as the repository
- Enter ``latest`` as the container tag
- Scroll to "Extra environment variables"
  - Add ``Grocy__BaseUrl`` and set it's value to your Grocy URL
  - Add ``Grocy__ApiKey`` and set it to your API token
- Scroll to "Networking and services", select http as the type and enter 80 for both target and port.
- (Optional): Set up ingress
- Scroll to the bottom and click "Install"