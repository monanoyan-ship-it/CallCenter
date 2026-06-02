function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function ProductsViewModel() {
    var self = this;
    var root = document.getElementById('products-vm');
    self.hasStockFinancePro = ko.observable(((root && root.dataset.stockFinancePro) || '').toLowerCase() === 'true');
    self.products = ko.observableArray([]);
    self.categories = ko.observableArray([]);
    self.brands = ko.observableArray([]);
    self.suppliers = ko.observableArray([]);
    self.branches = ko.observableArray([]);
    self.currentBranchId = ko.observable((window.slnGetBranch && window.slnGetBranch()) || '');
    self.branchTargetOptions = ko.computed(function () {
        if (window.slnBuildBranchTargetOptions) return window.slnBuildBranchTargetOptions(self.branches());
        return [{ id: '__all__', name: slnJsT('salon.common.all_branches', 'Tum Subeler') }].concat(self.branches() || []);
    });
    self.lowStockProducts = ko.observableArray([]);
    self.supplierOrders = ko.observableArray([]);
    self.activeProductsTab = ko.observable('products');
    self.searchQuery = ko.observable('');
    self.brandSearchQuery = ko.observable('');
    self.selectedCategoryName = ko.observable(null);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);
    self.isBrandSaving = ko.observable(false);
    self.isPurchaseSaving = ko.observable(false);
    self.isStockOperationSaving = ko.observable(false);
    self.isSupplierOrderSaving = ko.observable(false);
    self.purchaseProduct = ko.observable(null);
    self.stockOperationProduct = ko.observable(null);
    self.stockOperationMode = ko.observable('transfer');
    self.supplierOrderProduct = ko.observable(null);

    self.form = {
        name: ko.observable(''),
        categoryId: ko.observable(null),
        brandId: ko.observable(null),
        barcode: ko.observable(''),
        unit: ko.observable('Adet'),
        stockQuantity: ko.observable(0),
        branchTarget: ko.observable(null),
        minStockLevel: ko.observable(0),
        purchasePrice: ko.observable(0),
        salePrice: ko.observable(0)
    };

    self.brandForm = {
        id: ko.observable(null),
        name: ko.observable('')
    };

    self.purchaseForm = {
        supplierId: ko.observable(null),
        branchId: ko.observable(null),
        quantity: ko.observable(1),
        unitPrice: ko.observable(0),
        notes: ko.observable('')
    };

    self.stockOperationForm = {
        fromBranchId: ko.observable(null),
        toBranchId: ko.observable(null),
        branchId: ko.observable(null),
        quantity: ko.observable(1),
        countedQuantity: ko.observable(0),
        notes: ko.observable('')
    };

    self.supplierOrderForm = {
        supplierId: ko.observable(null),
        quantity: ko.observable(1),
        unitPrice: ko.observable(0),
        expectedDate: ko.observable(''),
        notes: ko.observable('')
    };

    // ═══ Autocomplete'ler ═══
    self.categoryAutocomplete = createAutocomplete(self.categories, 'name', self.form.categoryId);
    self.brandAutocomplete = createAutocomplete(self.brands, 'name', self.form.brandId);

    self.filteredProducts = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        var catName = self.selectedCategoryName();
        return self.products().filter(function (p) {
            var matchQ = !q || (p.name || '').toLowerCase().indexOf(q) >= 0 || (p.barcode || '').indexOf(q) >= 0;
            var matchCat = !catName || p.categoryName === catName;
            return matchQ && matchCat;
        });
    });

    self.filteredBrands = ko.computed(function () {
        var q = (self.brandSearchQuery() || '').toLowerCase();
        return self.brands().filter(function (b) {
            return !q || (b.name || '').toLowerCase().indexOf(q) >= 0;
        });
    });

    self.brandProductCount = function (brand) {
        if (!brand) return 0;
        return self.products().filter(function (p) {
            if (p.brandId) return p.brandId == brand.id;
            return (p.brandName || '').toLowerCase() === (brand.name || '').toLowerCase();
        }).length;
    };

    self.showProductsTab = function () { self.activeProductsTab('products'); };
    self.showBrandsTab = function () { self.activeProductsTab('brands'); };

    self.hasMultipleBranches = ko.computed(function () {
        return self.branches().length > 1;
    });

    self.isAllBranchesStockContext = ko.computed(function () {
        return self.hasMultipleBranches() && !getCurrentBranchId();
    });

    self.purchaseTotal = ko.computed(function () {
        var quantity = parseFloat(self.purchaseForm.quantity()) || 0;
        var unitPrice = parseFloat(self.purchaseForm.unitPrice()) || 0;
        return quantity * unitPrice;
    });

    self.supplierOrderTotal = ko.computed(function () {
        var quantity = parseFloat(self.supplierOrderForm.quantity()) || 0;
        var unitPrice = parseFloat(self.supplierOrderForm.unitPrice()) || 0;
        return quantity * unitPrice;
    });

    var formModal;
    var brandModal;
    var purchaseModal;
    var stockOperationModal;
    var supplierOrderModal;

    function requireStockFinancePro() {
        if (self.hasStockFinancePro()) return true;
        toastr.info(slnJsT('salon.products.pro_required', 'Bu işlem için Stok Tedarik / Finans paketi gerekir.'));
        return false;
    }

    function getCurrentBranchId() {
        return parseInt(self.currentBranchId(), 10) || null;
    }

    function setDefaultBranch(observable) {
        var branches = self.branches();
        observable(getCurrentBranchId() || (branches.length === 1 ? branches[0].id : null));
    }

    function setDefaultBranchTarget(observable) {
        var branches = self.branches();
        observable(getCurrentBranchId() || (branches.length ? branches[0].id : ''));
    }

    function resolveBranchTarget(value) {
        if (window.slnResolveBranchTarget) {
            return window.slnResolveBranchTarget(value, 'salon.common.branch_target_required', 'Sube secin veya Tum Subeler secenegini secin');
        }

        if (value === '__all__') return { ok: true, branchId: null, allBranches: true };
        var branchId = parseInt(value, 10) || null;
        return branchId ? { ok: true, branchId: branchId, allBranches: false } : { ok: false };
    }

    function appendBranchTarget(url, target) {
        return window.slnAppendBranchTarget ? window.slnAppendBranchTarget(url, target) : url;
    }

    function requireBranchValue(branchId, fallback) {
        if (!self.hasMultipleBranches() || branchId) return true;
        toastr.warning(slnJsT('salon.products.stock_branch_required', fallback || 'Stok islemi icin sube secilmelidir'));
        return false;
    }

    function appendBranch(url, branchId) {
        if (!branchId) return url;
        return url + (url.indexOf('?') >= 0 ? '&' : '?') + 'branchId=' + encodeURIComponent(branchId);
    }

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-products', method: 'GET' }).done(function (data) {
            self.products(data.items || data);
        }).fail(function () {
            toastr.error(slnJsT('salon.products.js.load_failed', 'Ürünler yüklenemedi'));
        });
        self.loadLowStock();
        if (self.hasStockFinancePro()) {
            self.loadSupplierOrders();
        } else {
            self.supplierOrders([]);
        }
    };

    self.loadLowStock = function () {
        $.ajax({ url: '/proxy/sln-products/low-stock', method: 'GET' }).done(function (data) {
            self.lowStockProducts(data.items || data || []);
        });
    };

    self.loadSupplierOrders = function () {
        if (!self.hasStockFinancePro()) {
            self.supplierOrders([]);
            return;
        }
        $.ajax({ url: '/proxy/sln-products/supplier-orders', method: 'GET' }).done(function (data) {
            self.supplierOrders(data.items || data || []);
        });
    };

    self.loadLookups = function () {
        $.ajax({ url: '/proxy/sln-products/categories', method: 'GET' }).done(function (data) {
            self.categories(data);
        });
        $.ajax({ url: '/proxy/sln-products/brands', method: 'GET' }).done(function (data) {
            self.brands(data);
        });
        if (self.hasStockFinancePro()) {
            $.ajax({ url: '/proxy/sln-products/suppliers', method: 'GET' }).done(function (data) {
                self.suppliers(data.items || data);
            });
        } else {
            self.suppliers([]);
        }
        $.ajax({ url: '/proxy/sln-branches?_nb=1', method: 'GET' }).done(function (data) {
            self.branches(data.items || data || []);
        });
    };

    self.resetForm = function () {
        self.form.name('');
        self.form.categoryId(null);
        self.form.brandId(null);
        self.form.barcode('');
        self.form.unit('Adet');
        self.form.stockQuantity(0);
        setDefaultBranchTarget(self.form.branchTarget);
        self.form.minStockLevel(0);
        self.form.purchasePrice(0);
        self.form.salePrice(0);
        self.isEditing(false);
        self.editingId(null);
        self.categoryAutocomplete.clear();
        self.brandAutocomplete.clear();
    };

    self.openNew = function () {
        self.resetForm();
        formModal.show();
    };

    self.openPurchase = function (product) {
        if (!requireStockFinancePro()) return;
        self.purchaseProduct(product);
        self.purchaseForm.supplierId(null);
        setDefaultBranch(self.purchaseForm.branchId);
        self.purchaseForm.quantity(1);
        self.purchaseForm.unitPrice(product.purchasePrice || 0);
        self.purchaseForm.notes('');
        purchaseModal.show();
    };

    self.openStockTransfer = function (product) {
        if (!requireStockFinancePro()) return;
        self.stockOperationProduct(product);
        self.stockOperationMode('transfer');
        setDefaultBranch(self.stockOperationForm.fromBranchId);
        self.stockOperationForm.toBranchId(null);
        self.stockOperationForm.branchId(null);
        self.stockOperationForm.quantity(1);
        self.stockOperationForm.countedQuantity(product.stockQuantity || 0);
        self.stockOperationForm.notes('');
        stockOperationModal.show();
    };

    self.openStockCount = function (product) {
        if (!requireStockFinancePro()) return;
        self.stockOperationProduct(product);
        self.stockOperationMode('count');
        self.stockOperationForm.fromBranchId(null);
        self.stockOperationForm.toBranchId(null);
        setDefaultBranch(self.stockOperationForm.branchId);
        self.stockOperationForm.quantity(1);
        self.stockOperationForm.countedQuantity(product.stockQuantity || 0);
        self.stockOperationForm.notes('');
        stockOperationModal.show();
    };

    self.openSupplierOrder = function (product) {
        if (!requireStockFinancePro()) return;
        var normalized = {
            productId: product.productId || product.id,
            productName: product.productName || product.name,
            stockQuantity: product.stockQuantity || 0,
            minStockLevel: product.minStockLevel || 0,
            suggestedOrderQuantity: product.suggestedOrderQuantity || 1,
            purchasePrice: product.purchasePrice || 0,
            unit: product.unit || ''
        };
        self.supplierOrderProduct(normalized);
        self.supplierOrderForm.supplierId(null);
        self.supplierOrderForm.quantity(normalized.suggestedOrderQuantity || 1);
        self.supplierOrderForm.unitPrice(normalized.purchasePrice || 0);
        self.supplierOrderForm.expectedDate('');
        self.supplierOrderForm.notes('');
        supplierOrderModal.show();
    };

    self.openEdit = function (product) {
        self.isEditing(true);
        self.editingId(product.id);
        self.form.name(product.name || '');
        self.form.barcode(product.barcode || '');
        self.form.unit(product.unit || 'Adet');
        self.form.stockQuantity(product.stockQuantity || 0);
        self.form.branchTarget(product.branchId ? String(product.branchId) : (window.slnAllBranchesValue || '__all__'));
        self.form.minStockLevel(product.minStockLevel || 0);
        self.form.purchasePrice(product.purchasePrice || 0);
        self.form.salePrice(product.salePrice || 0);
        var matchedCat = self.categories().find(function (c) { return c.id === product.categoryId || c.name === product.categoryName; });
        var matchedBrand = self.brands().find(function (b) { return b.id === product.brandId || b.name === product.brandName; });
        var catId = matchedCat ? matchedCat.id : null;
        var brandId = matchedBrand ? matchedBrand.id : null;
        self.form.categoryId(catId);
        self.form.brandId(brandId);
        self.categoryAutocomplete.setFromValue(catId);
        self.brandAutocomplete.setFromValue(brandId);
        formModal.show();
    };

    self.openNewBrand = function () {
        self.brandForm.id(null);
        self.brandForm.name('');
        brandModal.show();
    };

    self.openEditBrand = function (brand) {
        self.brandForm.id(brand.id);
        self.brandForm.name(brand.name || '');
        brandModal.show();
    };

    self.saveBrand = function () {
        var name = (self.brandForm.name() || '').trim();
        if (!name) {
            toastr.warning(slnJsT('salon.products.brand_name_required', 'Marka adı zorunludur'));
            return;
        }

        self.isBrandSaving(true);
        var id = self.brandForm.id();
        var url = '/proxy/sln-products/brands';
        var method = 'POST';
        if (id) {
            url += '/' + id;
            method = 'PUT';
        }

        $.ajax({
            url: url,
            method: method,
            contentType: 'application/json',
            data: JSON.stringify({ name: name })
        }).done(function () {
            brandModal.hide();
            self.loadLookups();
            self.loadData();
            toastr.success(slnJsT('salon.products.brand_saved', 'Marka kaydedildi'));
        }).fail(function (xhr) {
            toastr.error(getErrorMessage(xhr, slnJsT('salon.products.brand_save_failed', 'Marka kaydedilemedi')));
        }).always(function () {
            self.isBrandSaving(false);
        });
    };

    self.removeBrand = function (brand) {
        confirmModal(
            slnJsT('salon.common.btn.confirm', 'Onayla'),
            slnJsT('salon.products.brand_delete_confirm', "'{name}' markasını silmek istediğinize emin misiniz?").replace('{name}', brand.name || ''),
            function () {
                $.ajax({ url: '/proxy/sln-products/brands/' + brand.id, method: 'DELETE' })
                    .done(function () {
                        self.loadLookups();
                        self.loadData();
                        toastr.success(slnJsT('salon.products.brand_deleted', 'Marka silindi'));
                    })
                    .fail(function (xhr) {
                        toastr.error(getErrorMessage(xhr, slnJsT('salon.products.brand_delete_failed', 'Marka silinemedi')));
                    });
            });
    };

    // Autocomplete'de secilmemis ama yazilmis isim varsa otomatik olustur
    function ensureLookup(autocomplete, formField, listObservable, createUrl, createErrorMessage) {
        return new Promise(function (resolve, reject) {
            // ID zaten secilmisse direkt don
            var selectedId = formField();
            if (selectedId) { resolve(selectedId); return; }

            // Yazilan text var mi?
            var text = (autocomplete.query() || '').trim();
            if (!text) { resolve(null); return; }

            // Listede var mi? (case-insensitive)
            var existing = listObservable().find(function (item) {
                return (item.name || '').toLowerCase() === text.toLowerCase();
            });
            if (existing) { resolve(existing.id); return; }

            // Yoksa olustur
            $.ajax({
                url: createUrl, method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ name: text, sortOrder: 0 })
            }).done(function (created) {
                if (!created || !created.id) {
                    reject(createErrorMessage || slnJsT('salon.common.error.generic', 'Bir hata oluştu'));
                    return;
                }
                var list = listObservable();
                list.push(created);
                listObservable(list);
                formField(created.id);
                autocomplete.setFromValue(created.id);
                resolve(created.id);
            }).fail(function (xhr) {
                reject(getErrorMessage(xhr, createErrorMessage || slnJsT('salon.common.error.generic', 'Bir hata oluştu')));
            });
        });
    }

    self.save = function () {
        if (!self.form.name()) { toastr.warning(slnJsT('salon.products.js.urun_adi_zorunludur', 'Ürün adi zorunludur')); return; }

        // Kategori zorunlu - secilmemis ve yazilmamissa uyar
        var catText = (self.categoryAutocomplete.query() || '').trim();
        if (!self.form.categoryId() && !catText) {
            toastr.warning(slnJsT('salon.products.js.kategori_zorunludur', 'Kategori zorunludur'));
            return;
        }

        var target = resolveBranchTarget(self.form.branchTarget());
        if (!target.ok) return;

        var stockQuantity = parseFloat(self.form.stockQuantity()) || 0;
        var minStockLevel = parseFloat(self.form.minStockLevel()) || 0;
        var purchasePrice = parseFloat(self.form.purchasePrice()) || 0;
        var salePrice = parseFloat(self.form.salePrice()) || 0;

        if (target.allBranches && stockQuantity !== 0) {
            toastr.warning(slnJsT('salon.products.all_branches_initial_stock_warning', 'Tum Subeler seciliyken baslangic stogu girilemez; stok miktarini sube bazli alis veya sayim ile girin'));
            return;
        }
        if (!target.allBranches && stockQuantity <= 0) {
            toastr.warning(slnJsT('salon.products.stock_quantity_positive', "Stok miktari 0'dan buyuk olmalidir"));
            return;
        }
        if (purchasePrice <= 0) {
            toastr.warning(slnJsT('salon.products.js.purchase_price_positive', "Alis fiyati 0'dan buyuk olmalidir"));
            return;
        }
        if (salePrice <= 0) {
            toastr.warning(slnJsT('salon.products.sale_price_positive', "Satis fiyati 0'dan buyuk olmalidir"));
            return;
        }

        self.isSaving(true);

        Promise.all([
            ensureLookup(self.categoryAutocomplete, self.form.categoryId, self.categories, '/proxy/sln-products/categories', slnJsT('salon.products.js.kategori_olusturulamadi', 'Kategori oluşturulamadı')),
            ensureLookup(self.brandAutocomplete, self.form.brandId, self.brands, '/proxy/sln-products/brands', slnJsT('salon.products.brand_create_failed', 'Marka oluşturulamadı'))
        ]).then(function (results) {
            if (!results[0]) {
                toastr.error(slnJsT('salon.products.js.kategori_olusturulamadi', 'Kategori oluşturulamadı'));
                self.isSaving(false);
                return;
            }

            var data = {
                name: self.form.name(),
                categoryId: results[0],
                brandId: results[1],
                barcode: self.form.barcode(),
                unit: self.form.unit(),
                stockQuantity: stockQuantity,
                minStockLevel: minStockLevel,
                purchasePrice: purchasePrice,
                salePrice: salePrice
            };

            var url = appendBranchTarget('/proxy/sln-products', target);
            var method = 'POST';
            if (self.isEditing()) {
                url = appendBranchTarget('/proxy/sln-products/' + self.editingId(), target);
                method = 'PUT';
            }

            $.ajax({
                url: url, method: method,
                contentType: 'application/json',
                data: JSON.stringify(data)
            }).done(function () {
                formModal.hide();
                self.loadData();
                self.loadLookups();
                toastr.success(self.isEditing() ? slnJsT('salon.products.js.urun_guncellendi', 'Ürün güncellendi') : slnJsT('salon.products.js.urun_eklendi', 'Ürün eklendi'));
            }).fail(function (xhr) {
                toastr.error(getErrorMessage(xhr, slnJsT('salon.common.error.generic', 'Bir hata oluştu')));
            }).always(function () { self.isSaving(false); });
        }).catch(function (error) {
            toastr.error(error || slnJsT('salon.common.error.generic', 'Bir hata oluştu'));
            self.isSaving(false);
        });
    };

    function getErrorMessage(xhr, fallback) {
        if (window.slnAjaxErrorMessage) return window.slnAjaxErrorMessage(xhr, fallback);
        if (xhr.responseJSON && xhr.responseJSON.message) return xhr.responseJSON.message;
        if (xhr.responseJSON && xhr.responseJSON.error) return xhr.responseJSON.error;
        if (xhr.responseJSON && xhr.responseJSON.detail) return xhr.responseJSON.detail;
        if (xhr.responseText) return xhr.responseText.replace(/^"|"$/g, '');
        return fallback;
    }

    self.savePurchase = function () {
        if (!requireStockFinancePro()) return;
        var product = self.purchaseProduct();
        if (!product) { return; }

        var supplierId = parseInt(self.purchaseForm.supplierId());
        var branchId = parseInt(self.purchaseForm.branchId(), 10) || null;
        var quantity = parseFloat(self.purchaseForm.quantity()) || 0;
        var unitPrice = parseFloat(self.purchaseForm.unitPrice()) || 0;

        if (!supplierId) { toastr.warning(slnJsT('salon.products.js.tedarikci_secilmelidir', 'Tedarikci secilmelidir')); return; }
        if (!requireBranchValue(branchId, slnJsT('salon.products.purchase_branch_required', 'Alis kaydi icin sube secilmelidir'))) return;
        if (quantity <= 0) { toastr.warning(slnJsT('salon.products.js.quantity_positive', "Miktar 0'dan büyük olmalıdır")); return; }
        if (unitPrice <= 0) { toastr.warning(slnJsT('salon.products.js.purchase_price_positive', "Alış fiyatı 0'dan büyük olmalıdır")); return; }

        self.isPurchaseSaving(true);

        $.ajax({
            url: '/proxy/sln-products/' + product.id + '/stock-movements',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                movementTypeId: 1,
                quantity: quantity,
                unitPrice: unitPrice,
                supplierId: supplierId,
                branchId: branchId,
                notes: self.purchaseForm.notes()
            })
        }).done(function () {
            purchaseModal.hide();
            self.loadData();
            toastr.success(slnJsT('salon.products.js.alis_kaydi_eklendi_tedarikci_carisi_guncellendi', 'Alis kaydi eklendi, tedarikci carisi güncellendi'));
        }).fail(function (xhr) {
            toastr.error(getErrorMessage(xhr, slnJsT('salon.products.js.purchase_save_failed', 'Alış kaydı eklenemedi')));
        }).always(function () {
            self.isPurchaseSaving(false);
        });
    };

    self.saveStockOperation = function () {
        if (!requireStockFinancePro()) return;
        var product = self.stockOperationProduct();
        if (!product) { return; }

        self.isStockOperationSaving(true);

        if (self.stockOperationMode() === 'transfer') {
            var fromBranchId = parseInt(self.stockOperationForm.fromBranchId(), 10) || null;
            var toBranchId = parseInt(self.stockOperationForm.toBranchId());
            var quantity = parseFloat(self.stockOperationForm.quantity()) || 0;
            if (!requireBranchValue(fromBranchId, slnJsT('salon.products.source_branch_required', 'Kaynak sube secilmelidir'))) {
                self.isStockOperationSaving(false);
                return;
            }
            if (!toBranchId) {
                toastr.warning(slnJsT('salon.products.js.hedef_sube_secilmelidir', 'Hedef şube secilmelidir'));
                self.isStockOperationSaving(false);
                return;
            }
            if (quantity <= 0) {
                toastr.warning(slnJsT('salon.products.js.transfer_quantity_positive', "Transfer miktarı 0'dan büyük olmalıdır"));
                self.isStockOperationSaving(false);
                return;
            }

            $.ajax({
                url: '/proxy/sln-products/' + product.id + '/stock-transfer',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    fromBranchId: fromBranchId,
                    toBranchId: toBranchId,
                    quantity: quantity,
                    notes: self.stockOperationForm.notes()
                })
            }).done(function () {
                stockOperationModal.hide();
                self.loadData();
                toastr.success(slnJsT('salon.products.js.transfer_audit_created', 'Stok transfer audit kaydı oluşturuldu'));
            }).fail(function (xhr) {
                toastr.error(getErrorMessage(xhr, slnJsT('salon.products.js.transfer_save_failed', 'Stok transferi kaydedilemedi')));
            }).always(function () {
                self.isStockOperationSaving(false);
            });
            return;
        }

        var countedQuantity = parseFloat(self.stockOperationForm.countedQuantity());
        var countBranchId = parseInt(self.stockOperationForm.branchId(), 10) || null;
        if (!requireBranchValue(countBranchId, slnJsT('salon.products.count_branch_required', 'Sayim icin sube secilmelidir'))) {
            self.isStockOperationSaving(false);
            return;
        }
        if (isNaN(countedQuantity) || countedQuantity < 0) {
            toastr.warning(slnJsT('salon.products.js.counted_stock_negative', 'Sayılan stok negatif olamaz'));
            self.isStockOperationSaving(false);
            return;
        }

        $.ajax({
            url: '/proxy/sln-products/' + product.id + '/stock-count',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                branchId: countBranchId,
                countedQuantity: countedQuantity,
                notes: self.stockOperationForm.notes()
            })
        }).done(function () {
            stockOperationModal.hide();
            self.loadData();
            toastr.success(slnJsT('salon.products.js.sayim_farki_kaydedildi', 'Sayim farki kaydedildi'));
        }).fail(function (xhr) {
            toastr.error(getErrorMessage(xhr, slnJsT('salon.products.js.stock_count_save_failed', 'Sayım farkı kaydedilemedi')));
        }).always(function () {
            self.isStockOperationSaving(false);
        });
    };

    self.saveSupplierOrder = function () {
        if (!requireStockFinancePro()) return;
        var product = self.supplierOrderProduct();
        if (!product) { return; }

        var supplierId = parseInt(self.supplierOrderForm.supplierId());
        var branchId = getCurrentBranchId();
        var quantity = parseFloat(self.supplierOrderForm.quantity()) || 0;
        var unitPrice = parseFloat(self.supplierOrderForm.unitPrice()) || 0;

        if (!supplierId) { toastr.warning(slnJsT('salon.products.js.tedarikci_secilmelidir', 'Tedarikci secilmelidir')); return; }
        if (!requireBranchValue(branchId, slnJsT('salon.products.purchase_branch_required', 'Alis kaydi icin sube secilmelidir'))) return;
        if (quantity <= 0) { toastr.warning(slnJsT('salon.products.js.order_quantity_positive', "Siparis miktari 0'dan buyuk olmalidir")); return; }
        if (unitPrice < 0) { toastr.warning(slnJsT('salon.products.js.unit_price_negative', 'Birim fiyat negatif olamaz')); return; }

        self.isSupplierOrderSaving(true);

        $.ajax({
            url: '/proxy/sln-products/supplier-orders',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                supplierId: supplierId,
                expectedDate: self.supplierOrderForm.expectedDate() || null,
                notes: self.supplierOrderForm.notes(),
                items: [{
                    productId: product.productId,
                    quantity: quantity,
                    unitPrice: unitPrice
                }]
            })
        }).done(function () {
            supplierOrderModal.hide();
            self.loadData();
            toastr.success(slnJsT('salon.products.js.supplier_order_created', 'Tedarik siparisi olusturuldu'));
        }).fail(function (xhr) {
            toastr.error(getErrorMessage(xhr, slnJsT('salon.products.js.supplier_order_save_failed', 'Tedarik siparişi oluşturulamadı')));
        }).always(function () {
            self.isSupplierOrderSaving(false);
        });
    };

    self.remove = function (product) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.products.js.delete_confirm', "'{name}' ürününü silmek istediğinize emin misiniz?").replace('{name}', product.name || ''), function() {
            $.ajax({ url: '/proxy/sln-products/' + product.id, method: 'DELETE' }).done(function () {
                self.loadData();
                toastr.success(slnJsT('salon.products.js.urun_silindi', 'Ürün silindi'));
            }).fail(function () {
                toastr.error(slnJsT('salon.products.js.urun_silinemedi', 'Ürün silinemedi'));
            });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('productModal'));
        brandModal = new bootstrap.Modal(document.getElementById('brandModal'));
        purchaseModal = new bootstrap.Modal(document.getElementById('purchaseModal'));
        stockOperationModal = new bootstrap.Modal(document.getElementById('stockOperationModal'));
        supplierOrderModal = new bootstrap.Modal(document.getElementById('supplierOrderModal'));
        self.loadLookups();
        self.loadData();
    });
}

ko.applyBindings(new ProductsViewModel(), document.getElementById('products-vm'));
