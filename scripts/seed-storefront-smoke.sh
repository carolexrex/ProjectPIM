#!/usr/bin/env bash
set -euo pipefail

# Seeds the local PostgreSQL development database with the minimum catalog data the
# storefront smoke needs, requests a storefront projection rebuild, and then runs the
# read smoke plus the cart/checkout gate described in docs/pim-roadmap-v1.md.
#
# This is the bash equivalent of scripts/seed-storefront-smoke.ps1 for macOS/Linux.
# Requires: bash, curl, jq.

ADMIN_API_URL="http://localhost:5053"
STOREFRONT_API_URL="http://localhost:5064"
USERNAME="test"
PASSWORD="Test123!"
CULTURE="sv-SE"
SKIP_PROJECTION_REBUILD=0
SKIP_SMOKE_CHECK=0
SKIP_CART_SMOKE=0
PROJECTION_WAIT_SECONDS=60

while [[ $# -gt 0 ]]; do
  case "$1" in
    --admin-url) ADMIN_API_URL="$2"; shift 2 ;;
    --storefront-url) STOREFRONT_API_URL="$2"; shift 2 ;;
    --username) USERNAME="$2"; shift 2 ;;
    --password) PASSWORD="$2"; shift 2 ;;
    --culture) CULTURE="$2"; shift 2 ;;
    --projection-wait-seconds) PROJECTION_WAIT_SECONDS="$2"; shift 2 ;;
    --skip-projection-rebuild) SKIP_PROJECTION_REBUILD=1; shift ;;
    --skip-smoke-check) SKIP_SMOKE_CHECK=1; shift ;;
    --skip-cart-smoke) SKIP_CART_SMOKE=1; shift ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done

ADMIN_API_URL="${ADMIN_API_URL%/}"
STOREFRONT_API_URL="${STOREFRONT_API_URL%/}"

for tool in curl jq; do
  command -v "$tool" >/dev/null 2>&1 || { echo "Required command '$tool' was not found in PATH." >&2; exit 1; }
done

AUTH_TOKEN=""

log() { printf '==> %s\n' "$*" >&2; }
info() { printf '%s\n' "$*" >&2; }
urlencode() { jq -rn --arg x "$1" '$x|@uri'; }

# curl_json METHOD URL BODY [extra curl args...]
# Echoes the response body to stdout; logs and fails on a non-2xx status.
curl_json() {
  local method="$1" url="$2" body="$3"; shift 3
  local response http_code payload
  if [[ -n "$body" ]]; then
    response=$(curl -sS -X "$method" "$url" -H "Content-Type: application/json" "$@" --data "$body" -w $'\n%{http_code}') \
      || { echo "Request transport failure: $method $url" >&2; return 1; }
  else
    response=$(curl -sS -X "$method" "$url" "$@" -w $'\n%{http_code}') \
      || { echo "Request transport failure: $method $url" >&2; return 1; }
  fi
  http_code="${response##*$'\n'}"
  payload="${response%$'\n'*}"
  if (( http_code < 200 || http_code >= 300 )); then
    echo "ERROR $method $url -> HTTP $http_code: $payload" >&2
    return 1
  fi
  printf '%s' "$payload"
}

# http_status METHOD URL BODY [extra curl args...] -> prints status code only
http_status() {
  local method="$1" url="$2" body="$3"; shift 3
  if [[ -n "$body" ]]; then
    curl -sS -o /dev/null -X "$method" "$url" -H "Content-Type: application/json" "$@" --data "$body" -w '%{http_code}'
  else
    curl -sS -o /dev/null -X "$method" "$url" "$@" -w '%{http_code}'
  fi
}

admin() {
  local method="$1" path="$2" body="${3-}"
  curl_json "$method" "${ADMIN_API_URL}${path}" "$body" -H "Authorization: Bearer ${AUTH_TOKEN}"
}

# find_paged PATH SEARCH PROPERTY VALUE [EXTRA_QS]
# Echoes the matching item object (or nothing) from a paged admin list.
find_paged() {
  local path="$1" search="$2" prop="$3" value="$4" extra="${5-}"
  local url="${ADMIN_API_URL}${path}?search=$(urlencode "$search")&page=1&pageSize=50"
  [[ -n "$extra" ]] && url="${url}&${extra}"
  local res
  res=$(curl_json GET "$url" "" -H "Authorization: Bearer ${AUTH_TOKEN}") || return 1
  jq -c --arg p "$prop" --arg v "$value" '.items[]? | select((.[$p]|tostring)==$v)' <<<"$res" | head -n1
}

require_status() {
  # require_status ENTITY_TYPE CODE -> echoes the status id
  local entity="$1" code="$2" res id
  res=$(admin GET "/api/admin/product-statuses?entityType=$(urlencode "$entity")") || return 1
  id=$(jq -r --arg c "$code" '.[] | select(.code==$c) | .id' <<<"$res" | head -n1)
  if [[ -z "$id" || "$id" == "null" ]]; then
    echo "Required $entity status '$code' was not found. Apply migrations before seeding smoke data." >&2
    return 1
  fi
  printf '%s' "$id"
}

# ---------------------------------------------------------------------------
# Login
# ---------------------------------------------------------------------------
log "Logging into admin API as ${USERNAME}"
login=$(curl_json POST "${ADMIN_API_URL}/api/admin/auth/login" \
  "$(jq -nc --arg u "$USERNAME" --arg p "$PASSWORD" '{username:$u,password:$p}')")
AUTH_TOKEN=$(jq -r '.accessToken' <<<"$login")
[[ -n "$AUTH_TOKEN" && "$AUTH_TOKEN" != "null" ]] || { echo "Login did not return an access token." >&2; exit 1; }

PRODUCT_STATUS_ID=$(require_status Product READY)
VARIANT_STATUS_ID=$(require_status Variant READY)

# ---------------------------------------------------------------------------
# Market
# ---------------------------------------------------------------------------
existing=$(find_paged "/api/admin/markets" "SE" "code" "SE")
if [[ -z "$existing" ]]; then
  log "Creating market SE"
  market=$(admin POST "/api/admin/markets" \
    '{"code":"SE","name":"Sweden","defaultCurrency":"SEK","defaultCulture":"sv-SE","vatMode":"Gross"}')
else
  log "Using existing market SE"
  market=$(admin GET "/api/admin/markets/$(jq -r '.id' <<<"$existing")")
fi
MARKET_ID=$(jq -r '.id' <<<"$market")

market=$(admin PUT "/api/admin/markets/${MARKET_ID}/currencies" \
  "$(jq -nc --argjson rv "$(jq '.rowVersion' <<<"$market")" '{defaultCurrency:"SEK",currencies:["SEK"],rowVersion:$rv}')")
market=$(admin PUT "/api/admin/markets/${MARKET_ID}/cultures" \
  "$(jq -nc --argjson rv "$(jq '.rowVersion' <<<"$market")" '{defaultCulture:"sv-SE",cultures:["sv-SE","en-GB"],rowVersion:$rv}')")

# ---------------------------------------------------------------------------
# Channel
# ---------------------------------------------------------------------------
existing=$(find_paged "/api/admin/channels" "WEB-SE" "code" "WEB-SE")
if [[ -z "$existing" ]]; then
  log "Creating channel WEB-SE"
  channel=$(admin POST "/api/admin/channels" \
    '{"code":"WEB-SE","name":"Swedish Web","hostName":"se.example.com"}')
else
  log "Using existing channel WEB-SE"
  channel=$(admin GET "/api/admin/channels/$(jq -r '.id' <<<"$existing")")
fi
CHANNEL_ID=$(jq -r '.id' <<<"$channel")
if [[ -z "$(jq -c --arg m "$MARKET_ID" '.markets[]? | select((.marketId|tostring)==$m)' <<<"$channel")" ]]; then
  log "Assigning market SE to channel WEB-SE"
  channel=$(admin POST "/api/admin/channels/${CHANNEL_ID}/markets" \
    "$(jq -nc --arg m "$MARKET_ID" --argjson rv "$(jq '.rowVersion' <<<"$channel")" '{marketId:$m,rowVersion:$rv}')")
fi

# ---------------------------------------------------------------------------
# Media asset
# ---------------------------------------------------------------------------
existing=$(find_paged "/api/admin/media-assets" "drill-hero.jpg" "fileName" "drill-hero.jpg")
if [[ -z "$existing" ]]; then
  log "Creating media asset drill-hero.jpg"
  media=$(admin POST "/api/admin/media-assets" \
    '{"storageProvider":"External","storageKey":"smoke/drill-hero.jpg","fileName":"drill-hero.jpg","contentType":"image/jpeg","fileSize":0,"width":1200,"height":1200,"publicUrl":"https://images.example.com/drill-hero.jpg","title":"Example Drill","altText":"Example drill hero image"}')
else
  log "Using existing media asset drill-hero.jpg"
  media=$(admin GET "/api/admin/media-assets/$(jq -r '.id' <<<"$existing")")
fi
MEDIA_ID=$(jq -r '.id' <<<"$media")

# ---------------------------------------------------------------------------
# Brand
# ---------------------------------------------------------------------------
existing=$(find_paged "/api/admin/brands" "ACME" "code" "ACME")
if [[ -z "$existing" ]]; then
  log "Creating brand ACME"
  brand=$(admin POST "/api/admin/brands" \
    "$(jq -nc --arg m "$MEDIA_ID" '{code:"ACME",websiteUrl:"https://www.example.com",logoMediaAssetId:$m,sortOrder:10}')")
else
  log "Using existing brand ACME"
  brand=$(admin GET "/api/admin/brands/$(jq -r '.id' <<<"$existing")")
fi
BRAND_ID=$(jq -r '.id' <<<"$brand")
admin PUT "/api/admin/brands/${BRAND_ID}/translations/en-GB" \
  '{"name":"Acme Tools","slug":"acme-tools","description":"Sample brand for storefront smoke data."}' >/dev/null
admin PUT "/api/admin/brands/${BRAND_ID}/translations/sv-SE" \
  '{"name":"Acme Verktyg","slug":"acme-verktyg","description":"Varumarke for storefront smoke data."}' >/dev/null

# ---------------------------------------------------------------------------
# Categories
# ---------------------------------------------------------------------------
ensure_category() {
  # ensure_category CODE NAME_EN SLUG_EN DESC_EN NAME_SV SLUG_SV DESC_SV SORT PARENT_ID
  local code="$1" nameEn="$2" slugEn="$3" descEn="$4" nameSv="$5" slugSv="$6" descSv="$7" sort="$8" parent="${9-}"
  local existing cat id body
  existing=$(find_paged "/api/admin/categories" "$code" "code" "$code")
  if [[ -z "$existing" ]]; then
    log "Creating category $code"
    if [[ -n "$parent" ]]; then
      body=$(jq -nc --arg c "$code" --argjson s "$sort" --arg p "$parent" '{code:$c,sortOrder:$s,parentCategoryId:$p}')
    else
      body=$(jq -nc --arg c "$code" --argjson s "$sort" '{code:$c,sortOrder:$s}')
    fi
    cat=$(admin POST "/api/admin/categories" "$body")
  else
    log "Using existing category $code"
    cat=$(admin GET "/api/admin/categories/$(jq -r '.id' <<<"$existing")")
  fi
  id=$(jq -r '.id' <<<"$cat")
  admin PUT "/api/admin/categories/${id}/translations/en-GB" \
    "$(jq -nc --arg n "$nameEn" --arg s "$slugEn" --arg d "$descEn" '{name:$n,slug:$s,description:$d}')" >/dev/null
  admin PUT "/api/admin/categories/${id}/translations/sv-SE" \
    "$(jq -nc --arg n "$nameSv" --arg s "$slugSv" --arg d "$descSv" '{name:$n,slug:$s,description:$d}')" >/dev/null
  printf '%s' "$id"
}

TOOLS_ID=$(ensure_category "TOOLS" "Tools" "tools" "Catalog root for tools." "Verktyg" "verktyg" "Katalogrot for verktyg." 10)
DRILLS_ID=$(ensure_category "DRILLS" "Drills" "drills" "Electric and battery-powered drills." "Borrar" "borrar" "Elektriska och batteridrivna borrar." 20 "$TOOLS_ID")

# ---------------------------------------------------------------------------
# Attributes
# ---------------------------------------------------------------------------
ensure_attribute() {
  # ensure_attribute CODE NAME SCOPE IS_VARIANT_DEFINING OPTIONS_JSON -> echoes attribute object
  local code="$1" name="$2" scope="$3" defining="$4" options="$5" existing
  existing=$(find_paged "/api/admin/product-attributes" "$code" "code" "$code")
  if [[ -z "$existing" ]]; then
    log "Creating product attribute $code"
    admin POST "/api/admin/product-attributes" \
      "$(jq -nc --arg c "$code" --arg n "$name" --arg s "$scope" --argjson d "$defining" --argjson o "$options" \
        '{code:$c,name:$n,scope:$s,dataType:"Select",isVariantDefining:$d,isFilterable:true,isRequired:true,sortOrder:10,options:$o}')"
  else
    log "Using existing product attribute $code"
    admin GET "/api/admin/product-attributes/$(jq -r '.id' <<<"$existing")"
  fi
}

color=$(ensure_attribute "COLOR" "Color" "Variant" true \
  '[{"code":"BLACK","value":"Black","sortOrder":10},{"code":"RED","value":"Red","sortOrder":20}]')
power=$(ensure_attribute "POWER_SOURCE" "Power Source" "Product" false \
  '[{"code":"CORDED","value":"Corded","sortOrder":10},{"code":"CORDLESS","value":"Cordless","sortOrder":20}]')

COLOR_ATTR_ID=$(jq -r '.id' <<<"$color")
POWER_ATTR_ID=$(jq -r '.id' <<<"$power")
BLACK_OPTION_ID=$(jq -r '.options[] | select(.code=="BLACK") | .id' <<<"$color" | head -n1)
CORDED_OPTION_ID=$(jq -r '.options[] | select(.code=="CORDED") | .id' <<<"$power" | head -n1)
[[ -n "$BLACK_OPTION_ID" && "$BLACK_OPTION_ID" != "null" ]] || { echo "COLOR attribute is missing option BLACK." >&2; exit 1; }
[[ -n "$CORDED_OPTION_ID" && "$CORDED_OPTION_ID" != "null" ]] || { echo "POWER_SOURCE attribute is missing option CORDED." >&2; exit 1; }

# ---------------------------------------------------------------------------
# Product
# ---------------------------------------------------------------------------
existing=$(find_paged "/api/admin/products" "SKU-EXAMPLE-1" "productNumber" "SKU-EXAMPLE-1")
if [[ -z "$existing" ]]; then
  log "Creating product SKU-EXAMPLE-1"
  product=$(admin POST "/api/admin/products" \
    "$(jq -nc --arg brand "$BRAND_ID" --arg status "$PRODUCT_STATUS_ID" --arg cat "$DRILLS_ID" \
        --arg attr "$POWER_ATTR_ID" --arg opt "$CORDED_OPTION_ID" '{
      productType:"Hardware",productNumber:"SKU-EXAMPLE-1",slug:"example-drill",
      brandId:$brand,productStatusDefinitionId:$status,taxCategoryCode:"STANDARD",unitOfMeasure:"pcs",
      hasVariants:true,weight:1.8,length:28.0,width:8.0,height:22.0,categoryIds:[$cat],
      attributeValues:[{productAttributeId:$attr,attributeOptionId:$opt,valueText:null}]}')")
else
  log "Using existing product SKU-EXAMPLE-1"
  product=$(admin GET "/api/admin/products/$(jq -r '.id' <<<"$existing")")
fi
PRODUCT_ID=$(jq -r '.id' <<<"$product")
admin PUT "/api/admin/products/${PRODUCT_ID}/translations/en-GB" \
  '{"name":"Example Drill","shortDescription":"Compact and powerful drill for demanding work.","longDescription":"A compact drill designed for demanding work and reliable day-to-day use.","seoTitle":"Example Drill | Demo","seoDescription":"Compact and powerful drill for demanding work."}' >/dev/null
admin PUT "/api/admin/products/${PRODUCT_ID}/translations/sv-SE" \
  '{"name":"Exempelborr","shortDescription":"Kompakt och kraftfull borr for kravande arbete.","longDescription":"En kompakt borr for kravande arbete och palitlig daglig anvandning.","seoTitle":"Exempelborr | Demo","seoDescription":"Kompakt och kraftfull borr for kravande arbete."}' >/dev/null
product=$(admin GET "/api/admin/products/${PRODUCT_ID}")

# Product media
if [[ -z "$(jq -c --arg m "$MEDIA_ID" '.media[]? | select((.mediaAssetId|tostring)==$m)' <<<"$product")" ]]; then
  log "Assigning product media"
  product=$(admin POST "/api/admin/products/${PRODUCT_ID}/media" \
    "$(jq -nc --arg m "$MEDIA_ID" --argjson rv "$(jq '.rowVersion' <<<"$product")" \
      '{mediaAssetId:$m,type:"Image",sortOrder:10,isPrimary:true,rowVersion:$rv}')")
fi

# ---------------------------------------------------------------------------
# Variant
# ---------------------------------------------------------------------------
lookup=$(curl_json GET \
  "${ADMIN_API_URL}/api/admin/variants/lookup?search=$(urlencode SKU-EXAMPLE-1-BLACK)&status=Active&productId=${PRODUCT_ID}" \
  "" -H "Authorization: Bearer ${AUTH_TOKEN}")
existing_variant=$(jq -c '.[]? | select(.sku=="SKU-EXAMPLE-1-BLACK")' <<<"$lookup" | head -n1)
if [[ -z "$existing_variant" ]]; then
  log "Creating variant SKU-EXAMPLE-1-BLACK"
  variant=$(admin POST "/api/admin/products/${PRODUCT_ID}/variants" \
    "$(jq -nc --arg status "$VARIANT_STATUS_ID" --arg attr "$COLOR_ATTR_ID" --arg opt "$BLACK_OPTION_ID" '{
      sku:"SKU-EXAMPLE-1-BLACK",ean:"1234567890123",mpn:"ACME-DRILL-BLK",barcode:"1234567890123",
      productStatusDefinitionId:$status,isDefaultVariant:true,weight:1.8,length:28.0,width:8.0,height:22.0,
      attributeValues:[{productAttributeId:$attr,attributeOptionId:$opt,valueText:null}]}')")
else
  log "Using existing variant SKU-EXAMPLE-1-BLACK"
  variant=$(admin GET "/api/admin/variants/$(jq -r '.id' <<<"$existing_variant")")
fi
VARIANT_ID=$(jq -r '.id' <<<"$variant")

# Variant media
if [[ -z "$(jq -c --arg m "$MEDIA_ID" '.media[]? | select((.mediaAssetId|tostring)==$m)' <<<"$variant")" ]]; then
  log "Assigning variant media"
  variant=$(admin POST "/api/admin/variants/${VARIANT_ID}/media" \
    "$(jq -nc --arg m "$MEDIA_ID" --argjson rv "$(jq '.rowVersion' <<<"$variant")" \
      '{mediaAssetId:$m,type:"Image",sortOrder:10,isPrimary:true,rowVersion:$rv}')")
fi

# ---------------------------------------------------------------------------
# Market product assignment
# ---------------------------------------------------------------------------
market=$(admin GET "/api/admin/markets/${MARKET_ID}")
if [[ -z "$(jq -c --arg p "$PRODUCT_ID" '.productAssignments[]? | select((.productId|tostring)==$p and .status=="Active")' <<<"$market")" ]]; then
  log "Assigning product to market SE"
  market=$(admin PUT "/api/admin/markets/${MARKET_ID}/products/${PRODUCT_ID}" \
    "$(jq -nc --argjson rv "$(jq '.rowVersion' <<<"$market")" '{status:"Active",rowVersion:$rv}')")
fi

# ---------------------------------------------------------------------------
# Price list
# ---------------------------------------------------------------------------
existing=$(find_paged "/api/admin/price-lists" "SE_BASE_GROSS" "code" "SE_BASE_GROSS" "currencyCode=SEK")
if [[ -z "$existing" ]]; then
  log "Creating price list SE_BASE_GROSS"
  pricelist=$(admin POST "/api/admin/price-lists" \
    '{"code":"SE_BASE_GROSS","name":"SE Base Gross","currencyCode":"SEK","vatIncluded":true,"validFromUtc":null,"validToUtc":null}')
else
  log "Using existing price list SE_BASE_GROSS"
  pricelist=$(admin GET "/api/admin/price-lists/$(jq -r '.id' <<<"$existing")")
fi
PRICELIST_ID=$(jq -r '.id' <<<"$pricelist")
if [[ -z "$(jq -c --arg m "$MARKET_ID" '.markets[]? | select((.marketId|tostring)==$m)' <<<"$pricelist")" ]]; then
  pricelist=$(admin POST "/api/admin/price-lists/${PRICELIST_ID}/markets" \
    "$(jq -nc --arg m "$MARKET_ID" --argjson rv "$(jq '.rowVersion' <<<"$pricelist")" \
      '{marketId:$m,priority:0,isBasePriceList:true,rowVersion:$rv}')")
fi
entry_id=$(jq -r --arg v "$VARIANT_ID" '.entries[]? | select(.targetType=="Variant" and (.targetId|tostring)==$v and .minQuantity==1) | .id' <<<"$pricelist" | head -n1)
log "Upserting variant price"
admin POST "/api/admin/price-lists/${PRICELIST_ID}/entries" \
  "$(jq -nc --arg v "$VARIANT_ID" --argjson rv "$(jq '.rowVersion' <<<"$pricelist")" \
      --argjson eid "$([[ -n "$entry_id" && "$entry_id" != "null" ]] && jq -nc --arg e "$entry_id" '$e' || echo null)" '{
    entryId:$eid,targetType:"Variant",targetId:$v,minQuantity:1,amount:1499.00,compareAtAmount:1699.00,
    validFromUtc:null,validToUtc:null,rowVersion:$rv}')" >/dev/null

# ---------------------------------------------------------------------------
# Inventory
# ---------------------------------------------------------------------------
existing=$(find_paged "/api/admin/inventory-locations" "MAIN" "code" "MAIN")
if [[ -z "$existing" ]]; then
  log "Creating inventory location MAIN"
  location=$(admin POST "/api/admin/inventory-locations" \
    '{"code":"MAIN","name":"Main Warehouse","type":"Warehouse","countryCode":"SE"}')
else
  log "Using existing inventory location MAIN"
  location=$(admin GET "/api/admin/inventory-locations/$(jq -r '.id' <<<"$existing")")
fi
LOCATION_ID=$(jq -r '.id' <<<"$location")
if [[ -z "$(jq -c --arg m "$MARKET_ID" '.markets[]? | select((.marketId|tostring)==$m)' <<<"$location")" ]]; then
  location=$(admin POST "/api/admin/inventory-locations/${LOCATION_ID}/markets" \
    "$(jq -nc --arg m "$MARKET_ID" --argjson rv "$(jq '.rowVersion' <<<"$location")" '{marketId:$m,priority:0,rowVersion:$rv}')")
fi
balance_rv=$(jq -r --arg v "$VARIANT_ID" '.balances[]? | select((.variantId|tostring)==$v) | .rowVersion' <<<"$location" | head -n1)
log "Upserting variant inventory balance"
admin PUT "/api/admin/inventory-balances" \
  "$(jq -nc --arg loc "$LOCATION_ID" --arg v "$VARIANT_ID" \
      --argjson rv "$([[ -n "$balance_rv" && "$balance_rv" != "null" ]] && jq -nc --arg r "$balance_rv" '$r' || echo null)" '{
    inventoryLocationId:$loc,variantId:$v,onHandQuantity:25,reservedQuantity:2,incomingQuantity:10,
    backorderable:false,rowVersion:$rv}')" >/dev/null

# ---------------------------------------------------------------------------
# Storefront projection rebuild
# ---------------------------------------------------------------------------
if [[ "$SKIP_PROJECTION_REBUILD" -eq 1 ]]; then
  log "Skipping storefront projection rebuild"
else
  log "Requesting storefront projection rebuild job"
  job=$(admin POST "/api/admin/storefront/projection-rebuild-jobs" '{}')
  JOB_ID=$(jq -r '.id' <<<"$job")
  if [[ "$PROJECTION_WAIT_SECONDS" -le 0 ]]; then
    info "Projection rebuild job queued: ${JOB_ID}"
  else
    deadline=$(( $(date +%s) + PROJECTION_WAIT_SECONDS ))
    status="Pending"
    while [[ $(date +%s) -lt $deadline ]]; do
      job=$(admin GET "/api/admin/integration-jobs/${JOB_ID}")
      status=$(jq -r '.status' <<<"$job")
      if [[ "$status" == "Completed" ]]; then
        info "Projection rebuild completed: $(jq -r '.resultSummary // empty' <<<"$job")"
        break
      fi
      if [[ "$status" == "Failed" ]]; then
        echo "Projection rebuild job failed: $(jq -r '.lastError // empty' <<<"$job")" >&2
        exit 1
      fi
      sleep 2
    done
    if [[ "$status" != "Completed" ]]; then
      info "WARNING: projection rebuild job ${JOB_ID} is still '${status}'. Ensure Platform.Worker is running, then check /api/admin/integration-jobs/${JOB_ID}."
    fi
  fi
fi

QS="channel=WEB-SE&market=SE&culture=${CULTURE}&currency=SEK"

# ---------------------------------------------------------------------------
# Read smoke
# ---------------------------------------------------------------------------
if [[ "$SKIP_SMOKE_CHECK" -eq 0 ]]; then
  log "Checking storefront context and product endpoints"
  context=$(curl_json GET "${STOREFRONT_API_URL}/api/storefront/context?${QS}" "")
  products=$(curl_json GET "${STOREFRONT_API_URL}/api/storefront/products?${QS}&page=1&pageSize=24" "")
  product=$(curl_json GET "${STOREFRONT_API_URL}/api/storefront/products/example-drill?${QS}" "")
  info "Context OK: channel=$(jq -r '.channel.code' <<<"$context"), market=$(jq -r '.market.code' <<<"$context"), culture=$(jq -r '.activeCultureCode' <<<"$context"), currency=$(jq -r '.activeCurrencyCode' <<<"$context")"
  info "Products OK: total=$(jq -r '.total' <<<"$products"), first=$(jq -r '.items[0].productNumber // "<none>"' <<<"$products")"
  info "Product OK: productNumber=$(jq -r '.productNumber' <<<"$product"), slug=$(jq -r '.slug' <<<"$product")"
fi

# ---------------------------------------------------------------------------
# Cart / checkout gate (docs/pim-roadmap-v1.md "Immediate Next Step")
# ---------------------------------------------------------------------------
if [[ "$SKIP_CART_SMOKE" -eq 1 ]]; then
  info ""
  info "Storefront smoke seed is ready. Cart/checkout gate skipped (--skip-cart-smoke)."
  exit 0
fi

CART_HEADER="X-Storefront-Cart-Token"
fail=0

log "Cart gate 1/8: resolving context"
curl_json GET "${STOREFRONT_API_URL}/api/storefront/context?${QS}" "" >/dev/null

log "Cart gate 2/8: reading product detail and selecting a buyable variant"
product=$(curl_json GET "${STOREFRONT_API_URL}/api/storefront/products/example-drill?${QS}" "")
variant_id=$(jq -r '(.variants[]? | select(.sku=="SKU-EXAMPLE-1-BLACK") | .id) // (.variants[0]?.id) // empty' <<<"$product")
[[ -n "$variant_id" ]] || { echo "Could not resolve a variant id from product detail. Did the projection rebuild complete?" >&2; exit 1; }
info "Selected variant: ${variant_id}"

log "Cart gate 3/8: creating cart"
cart_body=$(jq -nc --arg v "$variant_id" '{
  email:"smoke-buyer@example.com",
  lines:[{variantId:$v,quantity:1,comment:null}],
  addresses:[
    {type:"Billing",firstName:"Smoke",lastName:"Buyer",companyName:null,line1:"Test Street 1",line2:null,postalCode:"11122",city:"Stockholm",region:null,countryCode:"SE",email:"smoke-buyer@example.com",phone:"+46700000000"},
    {type:"Shipping",firstName:"Smoke",lastName:"Buyer",companyName:null,line1:"Test Street 1",line2:null,postalCode:"11122",city:"Stockholm",region:null,countryCode:"SE",email:"smoke-buyer@example.com",phone:"+46700000000"}
  ]}')
cart=$(curl_json POST "${STOREFRONT_API_URL}/api/storefront/carts?${QS}" "$cart_body")
CART_ID=$(jq -r '.id' <<<"$cart")
CART_TOKEN=$(jq -r '.cartAccessToken' <<<"$cart")
ROW_VERSION=$(jq -c '.rowVersion' <<<"$cart")
[[ -n "$CART_TOKEN" && "$CART_TOKEN" != "null" ]] || { echo "Cart creation did not return a cartAccessToken." >&2; exit 1; }
info "Created cart ${CART_ID} (token captured, grandTotal=$(jq -r '.grandTotal' <<<"$cart"))"

log "Cart gate 4/8: reading cart with ${CART_HEADER}"
curl_json GET "${STOREFRONT_API_URL}/api/storefront/carts/${CART_ID}" "" -H "${CART_HEADER}: ${CART_TOKEN}" >/dev/null

log "Cart gate 5/8: repricing cart"
cart=$(curl_json POST "${STOREFRONT_API_URL}/api/storefront/carts/${CART_ID}/reprice" \
  "$(jq -nc --argjson rv "$ROW_VERSION" '{rowVersion:$rv}')" -H "${CART_HEADER}: ${CART_TOKEN}")
ROW_VERSION=$(jq -c '.rowVersion' <<<"$cart")

log "Cart gate 6/8: checking out cart"
order=$(curl_json POST "${STOREFRONT_API_URL}/api/storefront/carts/${CART_ID}/checkout" \
  "$(jq -nc --argjson rv "$ROW_VERSION" '{rowVersion:$rv}')" -H "${CART_HEADER}: ${CART_TOKEN}")
ORDER_ID=$(jq -r '.id' <<<"$order")
info "Checkout produced order ${ORDER_ID} (number=$(jq -r '.orderNumber' <<<"$order"), status=$(jq -r '.status' <<<"$order"))"

log "Cart gate 7/8: repeating checkout to confirm idempotency by source cart id"
order2=$(curl_json POST "${STOREFRONT_API_URL}/api/storefront/carts/${CART_ID}/checkout" \
  "$(jq -nc --argjson rv "$ROW_VERSION" '{rowVersion:$rv}')" -H "${CART_HEADER}: ${CART_TOKEN}")
ORDER_ID2=$(jq -r '.id' <<<"$order2")
if [[ "$ORDER_ID" == "$ORDER_ID2" ]]; then
  info "Idempotency OK: repeat checkout returned the same order ${ORDER_ID2}"
else
  echo "Idempotency FAILED: first order ${ORDER_ID} != repeat order ${ORDER_ID2}" >&2
  fail=1
fi

log "Cart gate 8/8: confirming missing/invalid token returns 401"
missing_status=$(http_status GET "${STOREFRONT_API_URL}/api/storefront/carts/${CART_ID}" "")
invalid_status=$(http_status GET "${STOREFRONT_API_URL}/api/storefront/carts/${CART_ID}" "" -H "${CART_HEADER}: not-a-valid-token")
if [[ "$missing_status" == "401" && "$invalid_status" == "401" ]]; then
  info "Ownership proof OK: missing token -> ${missing_status}, invalid token -> ${invalid_status}"
else
  echo "Ownership proof FAILED: missing token -> ${missing_status}, invalid token -> ${invalid_status} (expected 401/401)" >&2
  fail=1
fi

info ""
if [[ "$fail" -eq 0 ]]; then
  info "Storefront cart/checkout gate PASSED. Safe to start the next commerce slice."
else
  echo "Storefront cart/checkout gate FAILED. See errors above." >&2
  exit 1
fi
