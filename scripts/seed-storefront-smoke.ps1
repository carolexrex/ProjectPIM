param(
    [string]$AdminApiUrl = "http://localhost:5053",
    [string]$StorefrontApiUrl = "http://localhost:5064",
    [string]$Username = "test",
    [string]$Password = "Test123!",
    [switch]$SkipProjectionRebuild,
    [switch]$SkipSmokeCheck,
    [int]$ProjectionWaitSeconds = 60
)

$ErrorActionPreference = "Stop"

$AdminApiUrl = $AdminApiUrl.TrimEnd("/")
$StorefrontApiUrl = $StorefrontApiUrl.TrimEnd("/")

function Write-Step {
    param([string]$Message)
    Write-Host "==> $Message"
}

function ConvertTo-BodyJson {
    param($Body)

    if ($null -eq $Body) {
        return $null
    }

    return ($Body | ConvertTo-Json -Depth 20)
}

function Get-ErrorBody {
    param($Exception)

    $response = $Exception.Response
    if ($null -eq $response) {
        return $Exception.Message
    }

    try {
        $stream = $response.GetResponseStream()
        if ($null -eq $stream) {
            return $Exception.Message
        }

        $reader = [System.IO.StreamReader]::new($stream)
        return $reader.ReadToEnd()
    }
    catch {
        return $Exception.Message
    }
}

function Invoke-JsonRequest {
    param(
        [ValidateSet("GET", "POST", "PUT")]
        [string]$Method,
        [string]$Url,
        $Body = $null,
        [hashtable]$Headers = @{}
    )

    $parameters = @{
        Method = $Method
        Uri = $Url
        Headers = $Headers
        ContentType = "application/json"
    }

    $json = ConvertTo-BodyJson -Body $Body
    if ($null -ne $json) {
        $parameters.Body = $json
    }

    try {
        return Invoke-RestMethod @parameters
    }
    catch {
        $body = Get-ErrorBody -Exception $_.Exception
        throw "$Method $Url failed. $body"
    }
}

function Get-QueryUrl {
    param(
        [string]$Path,
        [hashtable]$Query
    )

    $pairs = @()
    foreach ($entry in $Query.GetEnumerator()) {
        if ($null -ne $entry.Value -and "$($entry.Value)" -ne "") {
            $pairs += "$([Uri]::EscapeDataString($entry.Key))=$([Uri]::EscapeDataString("$($entry.Value)"))"
        }
    }

    if ($pairs.Count -eq 0) {
        return "${AdminApiUrl}${Path}"
    }

    return "${AdminApiUrl}${Path}?$($pairs -join "&")"
}

function Invoke-Admin {
    param(
        [ValidateSet("GET", "POST", "PUT")]
        [string]$Method,
        [string]$Path,
        $Body = $null
    )

    return Invoke-JsonRequest -Method $Method -Url "${AdminApiUrl}${Path}" -Body $Body -Headers $script:AuthHeaders
}

function Get-PagedItemByProperty {
    param(
        [string]$Path,
        [string]$Search,
        [string]$Property,
        [string]$Value,
        [hashtable]$ExtraQuery = @{}
    )

    $query = @{
        search = $Search
        page = 1
        pageSize = 50
    }

    foreach ($entry in $ExtraQuery.GetEnumerator()) {
        $query[$entry.Key] = $entry.Value
    }

    $result = Invoke-JsonRequest -Method GET -Url (Get-QueryUrl -Path $Path -Query $query) -Headers $script:AuthHeaders
    return @($result.items) | Where-Object { "$($_.$Property)" -eq $Value } | Select-Object -First 1
}

function Get-RequiredStatus {
    param(
        [string]$EntityType,
        [string]$Code
    )

    $statuses = Invoke-JsonRequest -Method GET -Url (Get-QueryUrl -Path "/api/admin/product-statuses" -Query @{ entityType = $EntityType }) -Headers $script:AuthHeaders
    $status = @($statuses) | Where-Object { $_.code -eq $Code } | Select-Object -First 1
    if ($null -eq $status) {
        throw "Required $EntityType status '$Code' was not found. Apply migrations before seeding smoke data."
    }

    return $status
}

function Ensure-Market {
    $existing = Get-PagedItemByProperty -Path "/api/admin/markets" -Search "SE" -Property "code" -Value "SE"
    if ($null -eq $existing) {
        Write-Step "Creating market SE"
        $market = Invoke-Admin -Method POST -Path "/api/admin/markets" -Body @{
            code = "SE"
            name = "Sweden"
            defaultCurrency = "SEK"
            defaultCulture = "sv-SE"
            vatMode = "Gross"
        }
    }
    else {
        Write-Step "Using existing market SE"
        $market = Invoke-Admin -Method GET -Path "/api/admin/markets/$($existing.id)"
    }

    $market = Invoke-Admin -Method PUT -Path "/api/admin/markets/$($market.id)/currencies" -Body @{
        defaultCurrency = "SEK"
        currencies = @("SEK")
        rowVersion = $market.rowVersion
    }

    return Invoke-Admin -Method PUT -Path "/api/admin/markets/$($market.id)/cultures" -Body @{
        defaultCulture = "sv-SE"
        cultures = @("sv-SE", "en-GB")
        rowVersion = $market.rowVersion
    }
}

function Ensure-Channel {
    param($Market)

    $existing = Get-PagedItemByProperty -Path "/api/admin/channels" -Search "WEB-SE" -Property "code" -Value "WEB-SE"
    if ($null -eq $existing) {
        Write-Step "Creating channel WEB-SE"
        $channel = Invoke-Admin -Method POST -Path "/api/admin/channels" -Body @{
            code = "WEB-SE"
            name = "Swedish Web"
            hostName = "se.example.com"
        }
    }
    else {
        Write-Step "Using existing channel WEB-SE"
        $channel = Invoke-Admin -Method GET -Path "/api/admin/channels/$($existing.id)"
    }

    $hasMarket = @($channel.markets) | Where-Object { "$($_.marketId)" -eq "$($Market.id)" } | Select-Object -First 1
    if ($null -eq $hasMarket) {
        $channel = Invoke-Admin -Method POST -Path "/api/admin/channels/$($channel.id)/markets" -Body @{
            marketId = $Market.id
            rowVersion = $channel.rowVersion
        }
    }

    return $channel
}

function Ensure-MediaAsset {
    $existing = Get-PagedItemByProperty -Path "/api/admin/media-assets" -Search "drill-hero.jpg" -Property "fileName" -Value "drill-hero.jpg"
    if ($null -ne $existing) {
        Write-Step "Using existing media asset drill-hero.jpg"
        return Invoke-Admin -Method GET -Path "/api/admin/media-assets/$($existing.id)"
    }

    Write-Step "Creating media asset drill-hero.jpg"
    return Invoke-Admin -Method POST -Path "/api/admin/media-assets" -Body @{
        storageProvider = "External"
        storageKey = "smoke/drill-hero.jpg"
        fileName = "drill-hero.jpg"
        contentType = "image/jpeg"
        fileSize = 0
        width = 1200
        height = 1200
        publicUrl = "https://images.example.com/drill-hero.jpg"
        title = "Example Drill"
        altText = "Example drill hero image"
    }
}

function Ensure-Brand {
    param($MediaAsset)

    $existing = Get-PagedItemByProperty -Path "/api/admin/brands" -Search "ACME" -Property "code" -Value "ACME"
    if ($null -eq $existing) {
        Write-Step "Creating brand ACME"
        $brand = Invoke-Admin -Method POST -Path "/api/admin/brands" -Body @{
            code = "ACME"
            websiteUrl = "https://www.example.com"
            logoMediaAssetId = $MediaAsset.id
            sortOrder = 10
        }
    }
    else {
        Write-Step "Using existing brand ACME"
        $brand = Invoke-Admin -Method GET -Path "/api/admin/brands/$($existing.id)"
    }

    Invoke-Admin -Method PUT -Path "/api/admin/brands/$($brand.id)/translations/en-GB" -Body @{
        name = "Acme Tools"
        slug = "acme-tools"
        description = "Sample brand for storefront smoke data."
    } | Out-Null

    Invoke-Admin -Method PUT -Path "/api/admin/brands/$($brand.id)/translations/sv-SE" -Body @{
        name = "Acme Verktyg"
        slug = "acme-verktyg"
        description = "Varumarke for storefront smoke data."
    } | Out-Null

    return Invoke-Admin -Method GET -Path "/api/admin/brands/$($brand.id)"
}

function Ensure-Category {
    param(
        [string]$Code,
        [string]$NameEn,
        [string]$SlugEn,
        [string]$DescriptionEn,
        [string]$NameSv,
        [string]$SlugSv,
        [string]$DescriptionSv,
        [Guid]$ParentCategoryId = [Guid]::Empty,
        [int]$SortOrder
    )

    $existing = Get-PagedItemByProperty -Path "/api/admin/categories" -Search $Code -Property "code" -Value $Code
    if ($null -eq $existing) {
        Write-Step "Creating category $Code"
        $body = @{
            code = $Code
            sortOrder = $SortOrder
        }
        if ($ParentCategoryId -ne [Guid]::Empty) {
            $body.parentCategoryId = $ParentCategoryId
        }

        $category = Invoke-Admin -Method POST -Path "/api/admin/categories" -Body $body
    }
    else {
        Write-Step "Using existing category $Code"
        $category = Invoke-Admin -Method GET -Path "/api/admin/categories/$($existing.id)"
    }

    Invoke-Admin -Method PUT -Path "/api/admin/categories/$($category.id)/translations/en-GB" -Body @{
        name = $NameEn
        slug = $SlugEn
        description = $DescriptionEn
    } | Out-Null

    Invoke-Admin -Method PUT -Path "/api/admin/categories/$($category.id)/translations/sv-SE" -Body @{
        name = $NameSv
        slug = $SlugSv
        description = $DescriptionSv
    } | Out-Null

    return Invoke-Admin -Method GET -Path "/api/admin/categories/$($category.id)"
}

function Ensure-Attribute {
    param(
        [string]$Code,
        [string]$Name,
        [string]$Scope,
        [bool]$IsVariantDefining,
        [object[]]$Options
    )

    $existing = Get-PagedItemByProperty -Path "/api/admin/product-attributes" -Search $Code -Property "code" -Value $Code
    if ($null -eq $existing) {
        Write-Step "Creating product attribute $Code"
        return Invoke-Admin -Method POST -Path "/api/admin/product-attributes" -Body @{
            code = $Code
            name = $Name
            scope = $Scope
            dataType = "Select"
            isVariantDefining = $IsVariantDefining
            isFilterable = $true
            isRequired = $true
            sortOrder = 10
            options = $Options
        }
    }

    Write-Step "Using existing product attribute $Code"
    return Invoke-Admin -Method GET -Path "/api/admin/product-attributes/$($existing.id)"
}

function Get-OptionId {
    param($Attribute, [string]$Code)

    $option = @($Attribute.options) | Where-Object { $_.code -eq $Code } | Select-Object -First 1
    if ($null -eq $option) {
        throw "Attribute '$($Attribute.code)' is missing option '$Code'."
    }

    return $option.id
}

function Ensure-Product {
    param(
        $Brand,
        $Category,
        $PowerSourceAttribute,
        [Guid]$CordedOptionId,
        $ProductStatus
    )

    $existing = Get-PagedItemByProperty -Path "/api/admin/products" -Search "SKU-EXAMPLE-1" -Property "productNumber" -Value "SKU-EXAMPLE-1"
    if ($null -eq $existing) {
        Write-Step "Creating product SKU-EXAMPLE-1"
        $product = Invoke-Admin -Method POST -Path "/api/admin/products" -Body @{
            productType = "Hardware"
            productNumber = "SKU-EXAMPLE-1"
            slug = "example-drill"
            brandId = $Brand.id
            productStatusDefinitionId = $ProductStatus.id
            taxCategoryCode = "STANDARD"
            unitOfMeasure = "pcs"
            hasVariants = $true
            weight = 1.8
            length = 28.0
            width = 8.0
            height = 22.0
            categoryIds = @($Category.id)
            attributeValues = @(
                @{
                    productAttributeId = $PowerSourceAttribute.id
                    attributeOptionId = $CordedOptionId
                    valueText = $null
                }
            )
        }
    }
    else {
        Write-Step "Using existing product SKU-EXAMPLE-1"
        $product = Invoke-Admin -Method GET -Path "/api/admin/products/$($existing.id)"
    }

    Invoke-Admin -Method PUT -Path "/api/admin/products/$($product.id)/translations/en-GB" -Body @{
        name = "Example Drill"
        shortDescription = "Compact and powerful drill for demanding work."
        longDescription = "A compact drill designed for demanding work and reliable day-to-day use."
        seoTitle = "Example Drill | Demo"
        seoDescription = "Compact and powerful drill for demanding work."
    } | Out-Null

    Invoke-Admin -Method PUT -Path "/api/admin/products/$($product.id)/translations/sv-SE" -Body @{
        name = "Exempelborr"
        shortDescription = "Kompakt och kraftfull borr for kravande arbete."
        longDescription = "En kompakt borr for kravande arbete och palitlig daglig anvandning."
        seoTitle = "Exempelborr | Demo"
        seoDescription = "Kompakt och kraftfull borr for kravande arbete."
    } | Out-Null

    return Invoke-Admin -Method GET -Path "/api/admin/products/$($product.id)"
}

function Ensure-ProductMedia {
    param($Product, $MediaAsset)

    $existing = @($Product.media) | Where-Object { "$($_.mediaAssetId)" -eq "$($MediaAsset.id)" } | Select-Object -First 1
    if ($null -ne $existing) {
        return $Product
    }

    Write-Step "Assigning product media"
    return Invoke-Admin -Method POST -Path "/api/admin/products/$($Product.id)/media" -Body @{
        mediaAssetId = $MediaAsset.id
        type = "Image"
        sortOrder = 10
        isPrimary = $true
        rowVersion = $Product.rowVersion
    }
}

function Ensure-Variant {
    param(
        $Product,
        $ColorAttribute,
        [Guid]$BlackOptionId,
        $VariantStatus
    )

    $lookupUrl = Get-QueryUrl -Path "/api/admin/variants/lookup" -Query @{
        search = "SKU-EXAMPLE-1-BLACK"
        status = "Active"
        productId = $Product.id
    }
    $existing = @(Invoke-JsonRequest -Method GET -Url $lookupUrl -Headers $script:AuthHeaders) |
        Where-Object { $_.sku -eq "SKU-EXAMPLE-1-BLACK" } |
        Select-Object -First 1

    if ($null -eq $existing) {
        Write-Step "Creating variant SKU-EXAMPLE-1-BLACK"
        return Invoke-Admin -Method POST -Path "/api/admin/products/$($Product.id)/variants" -Body @{
            sku = "SKU-EXAMPLE-1-BLACK"
            ean = "1234567890123"
            mpn = "ACME-DRILL-BLK"
            barcode = "1234567890123"
            productStatusDefinitionId = $VariantStatus.id
            isDefaultVariant = $true
            weight = 1.8
            length = 28.0
            width = 8.0
            height = 22.0
            attributeValues = @(
                @{
                    productAttributeId = $ColorAttribute.id
                    attributeOptionId = $BlackOptionId
                    valueText = $null
                }
            )
        }
    }

    Write-Step "Using existing variant SKU-EXAMPLE-1-BLACK"
    return Invoke-Admin -Method GET -Path "/api/admin/variants/$($existing.id)"
}

function Ensure-VariantMedia {
    param($Variant, $MediaAsset)

    $existing = @($Variant.media) | Where-Object { "$($_.mediaAssetId)" -eq "$($MediaAsset.id)" } | Select-Object -First 1
    if ($null -ne $existing) {
        return $Variant
    }

    Write-Step "Assigning variant media"
    return Invoke-Admin -Method POST -Path "/api/admin/variants/$($Variant.id)/media" -Body @{
        mediaAssetId = $MediaAsset.id
        type = "Image"
        sortOrder = 10
        isPrimary = $true
        rowVersion = $Variant.rowVersion
    }
}

function Ensure-MarketProductAssignment {
    param($Market, $Product)

    $market = Invoke-Admin -Method GET -Path "/api/admin/markets/$($Market.id)"
    $existing = @($market.productAssignments) | Where-Object { "$($_.productId)" -eq "$($Product.id)" } | Select-Object -First 1
    if ($null -ne $existing -and $existing.status -eq "Active") {
        return $market
    }

    Write-Step "Assigning product to market SE"
    return Invoke-Admin -Method PUT -Path "/api/admin/markets/$($market.id)/products/$($Product.id)" -Body @{
        status = "Active"
        rowVersion = $market.rowVersion
    }
}

function Ensure-PriceList {
    param($Market, $Variant)

    $existing = Get-PagedItemByProperty -Path "/api/admin/price-lists" -Search "SE_BASE_GROSS" -Property "code" -Value "SE_BASE_GROSS" -ExtraQuery @{ currencyCode = "SEK" }
    if ($null -eq $existing) {
        Write-Step "Creating price list SE_BASE_GROSS"
        $priceList = Invoke-Admin -Method POST -Path "/api/admin/price-lists" -Body @{
            code = "SE_BASE_GROSS"
            name = "SE Base Gross"
            currencyCode = "SEK"
            vatIncluded = $true
            validFromUtc = $null
            validToUtc = $null
        }
    }
    else {
        Write-Step "Using existing price list SE_BASE_GROSS"
        $priceList = Invoke-Admin -Method GET -Path "/api/admin/price-lists/$($existing.id)"
    }

    $marketAssignment = @($priceList.markets) | Where-Object { "$($_.marketId)" -eq "$($Market.id)" } | Select-Object -First 1
    if ($null -eq $marketAssignment) {
        $priceList = Invoke-Admin -Method POST -Path "/api/admin/price-lists/$($priceList.id)/markets" -Body @{
            marketId = $Market.id
            priority = 0
            isBasePriceList = $true
            rowVersion = $priceList.rowVersion
        }
    }

    $entry = @($priceList.entries) | Where-Object { $_.targetType -eq "Variant" -and "$($_.targetId)" -eq "$($Variant.id)" -and $_.minQuantity -eq 1 } | Select-Object -First 1
    Write-Step "Upserting variant price"
    return Invoke-Admin -Method POST -Path "/api/admin/price-lists/$($priceList.id)/entries" -Body @{
        entryId = if ($null -eq $entry) { $null } else { $entry.id }
        targetType = "Variant"
        targetId = $Variant.id
        minQuantity = 1
        amount = 1499.00
        compareAtAmount = 1699.00
        validFromUtc = $null
        validToUtc = $null
        rowVersion = $priceList.rowVersion
    }
}

function Ensure-Inventory {
    param($Market, $Variant)

    $existing = Get-PagedItemByProperty -Path "/api/admin/inventory-locations" -Search "MAIN" -Property "code" -Value "MAIN"
    if ($null -eq $existing) {
        Write-Step "Creating inventory location MAIN"
        $location = Invoke-Admin -Method POST -Path "/api/admin/inventory-locations" -Body @{
            code = "MAIN"
            name = "Main Warehouse"
            type = "Warehouse"
            countryCode = "SE"
        }
    }
    else {
        Write-Step "Using existing inventory location MAIN"
        $location = Invoke-Admin -Method GET -Path "/api/admin/inventory-locations/$($existing.id)"
    }

    $marketAssignment = @($location.markets) | Where-Object { "$($_.marketId)" -eq "$($Market.id)" } | Select-Object -First 1
    if ($null -eq $marketAssignment) {
        $location = Invoke-Admin -Method POST -Path "/api/admin/inventory-locations/$($location.id)/markets" -Body @{
            marketId = $Market.id
            priority = 0
            rowVersion = $location.rowVersion
        }
    }

    $balance = @($location.balances) | Where-Object { "$($_.variantId)" -eq "$($Variant.id)" } | Select-Object -First 1
    Write-Step "Upserting variant inventory balance"
    Invoke-Admin -Method PUT -Path "/api/admin/inventory-balances" -Body @{
        inventoryLocationId = $location.id
        variantId = $Variant.id
        onHandQuantity = 25
        reservedQuantity = 2
        incomingQuantity = 10
        backorderable = $false
        rowVersion = if ($null -eq $balance) { $null } else { $balance.rowVersion }
    } | Out-Null

    return Invoke-Admin -Method GET -Path "/api/admin/inventory-locations/$($location.id)"
}

function Invoke-ProjectionRebuild {
    if ($SkipProjectionRebuild) {
        Write-Step "Skipping storefront projection rebuild"
        return
    }

    Write-Step "Requesting storefront projection rebuild job"
    $job = Invoke-Admin -Method POST -Path "/api/admin/storefront/projection-rebuild-jobs" -Body @{}

    if ($ProjectionWaitSeconds -le 0) {
        Write-Host "Projection rebuild job queued: $($job.id)"
        return
    }

    $deadline = (Get-Date).AddSeconds($ProjectionWaitSeconds)
    while ((Get-Date) -lt $deadline) {
        $job = Invoke-Admin -Method GET -Path "/api/admin/integration-jobs/$($job.id)"
        if ($job.status -eq "Completed") {
            Write-Host "Projection rebuild completed: $($job.resultSummary)"
            return
        }

        if ($job.status -eq "Failed") {
            throw "Projection rebuild job failed: $($job.lastError)"
        }

        Start-Sleep -Seconds 2
    }

    Write-Warning "Projection rebuild job $($job.id) is still '$($job.status)'. Ensure Platform.Worker is running, then check /api/admin/integration-jobs/$($job.id)."
}

function Invoke-StorefrontSmokeCheck {
    if ($SkipSmokeCheck) {
        return
    }

    Write-Step "Checking storefront context and product endpoints"
    $contextUrl = "$StorefrontApiUrl/api/storefront/context?channel=WEB-SE&market=SE&culture=en-GB&currency=SEK"
    $productsUrl = "$StorefrontApiUrl/api/storefront/products?channel=WEB-SE&market=SE&culture=en-GB&currency=SEK&page=1&pageSize=24"
    $productUrl = "$StorefrontApiUrl/api/storefront/products/example-drill?channel=WEB-SE&market=SE&culture=en-GB&currency=SEK"

    $context = Invoke-JsonRequest -Method GET -Url $contextUrl
    $products = Invoke-JsonRequest -Method GET -Url $productsUrl
    $product = Invoke-JsonRequest -Method GET -Url $productUrl

    Write-Host "Context OK: channel=$($context.channel.code), market=$($context.market.code), culture=$($context.activeCultureCode), currency=$($context.activeCurrencyCode)"
    Write-Host "Products OK: total=$($products.total), first=$(@($products.items)[0].productNumber)"
    Write-Host "Product OK: productNumber=$($product.productNumber), slug=$($product.slug)"
}

Write-Step "Logging into admin API"
$login = Invoke-JsonRequest -Method POST -Url "$AdminApiUrl/api/admin/auth/login" -Body @{
    username = $Username
    password = $Password
}

$script:AuthHeaders = @{
    Authorization = "Bearer $($login.accessToken)"
}

$readyProductStatus = Get-RequiredStatus -EntityType "Product" -Code "READY"
$readyVariantStatus = Get-RequiredStatus -EntityType "Variant" -Code "READY"

$market = Ensure-Market
$channel = Ensure-Channel -Market $market
$mediaAsset = Ensure-MediaAsset
$brand = Ensure-Brand -MediaAsset $mediaAsset
$tools = Ensure-Category -Code "TOOLS" -NameEn "Tools" -SlugEn "tools" -DescriptionEn "Catalog root for tools." -NameSv "Verktyg" -SlugSv "verktyg" -DescriptionSv "Katalogrot for verktyg." -SortOrder 10
$drills = Ensure-Category -Code "DRILLS" -NameEn "Drills" -SlugEn "drills" -DescriptionEn "Electric and battery-powered drills." -NameSv "Borrar" -SlugSv "borrar" -DescriptionSv "Elektriska och batteridrivna borrar." -ParentCategoryId $tools.id -SortOrder 20

$colorAttribute = Ensure-Attribute -Code "COLOR" -Name "Color" -Scope "Variant" -IsVariantDefining $true -Options @(
    @{ code = "BLACK"; value = "Black"; sortOrder = 10 },
    @{ code = "RED"; value = "Red"; sortOrder = 20 }
)
$powerSourceAttribute = Ensure-Attribute -Code "POWER_SOURCE" -Name "Power Source" -Scope "Product" -IsVariantDefining $false -Options @(
    @{ code = "CORDED"; value = "Corded"; sortOrder = 10 },
    @{ code = "CORDLESS"; value = "Cordless"; sortOrder = 20 }
)

$blackOptionId = Get-OptionId -Attribute $colorAttribute -Code "BLACK"
$cordedOptionId = Get-OptionId -Attribute $powerSourceAttribute -Code "CORDED"

$product = Ensure-Product -Brand $brand -Category $drills -PowerSourceAttribute $powerSourceAttribute -CordedOptionId $cordedOptionId -ProductStatus $readyProductStatus
$product = Ensure-ProductMedia -Product $product -MediaAsset $mediaAsset
$variant = Ensure-Variant -Product $product -ColorAttribute $colorAttribute -BlackOptionId $blackOptionId -VariantStatus $readyVariantStatus
$variant = Ensure-VariantMedia -Variant $variant -MediaAsset $mediaAsset
$market = Ensure-MarketProductAssignment -Market $market -Product $product
$priceList = Ensure-PriceList -Market $market -Variant $variant
$inventoryLocation = Ensure-Inventory -Market $market -Variant $variant

Invoke-ProjectionRebuild
Invoke-StorefrontSmokeCheck

Write-Host ""
Write-Host "Storefront smoke seed is ready for Nexra."
Write-Host "Context:          $StorefrontApiUrl/api/storefront/context?channel=WEB-SE&market=SE&culture=en-GB&currency=SEK"
Write-Host "Categories:       $StorefrontApiUrl/api/storefront/categories?channel=WEB-SE&market=SE&culture=en-GB&currency=SEK"
Write-Host "Products:         $StorefrontApiUrl/api/storefront/products?channel=WEB-SE&market=SE&culture=en-GB&currency=SEK&page=1&pageSize=24"
Write-Host "Product detail:   $StorefrontApiUrl/api/storefront/products/example-drill?channel=WEB-SE&market=SE&culture=en-GB&currency=SEK"
Write-Host "Product number:   $StorefrontApiUrl/api/storefront/products/by-number/SKU-EXAMPLE-1?channel=WEB-SE&market=SE&culture=en-GB&currency=SEK"
