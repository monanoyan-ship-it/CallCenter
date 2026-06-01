function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function SalesViewModel() {
    var self = this;
    self.categories = ko.observableArray([]);
    self.allServices = ko.observableArray([]);
    self.products = ko.observableArray([]);
    self.clientList = ko.observableArray([]);
    self.staffList = ko.observableArray([]);
    self.recipes = ko.observableArray([]);
    self.packageDefinitions = ko.observableArray([]);
    self.clientPackages = ko.observableArray([]);
    // Sadakat: D (program odulleri) + C (puan bakiyesi)
    self.loyaltyRewards = ko.observableArray([]);
    self.loyaltyConfig = ko.observable(null);
    self.clientLoyaltyBalance = ko.observable(0);
    self.loyaltyPointsToRedeem = ko.observable(0);
    // Cok Seansli Hizmet (B): musteriye satilmis aktif planlar
    self.serviceSessionPlans = ko.observableArray([]);
    self.laserDevices = ko.observableArray([
        { id: 'Alexandrite', name: 'Alexandrite Lazer' },
        { id: 'Diode', name: 'Diode Lazer' },
        { id: 'Nd:YAG', name: 'Nd:YAG Lazer' },
        { id: 'IPL', name: 'IPL / Fotoepilasyon' },
        { id: 'Ice Diode', name: 'Buz Lazer / Ice Diode' },
        { id: 'Triple Wave', name: 'Triple Wave / Hibrit Lazer' },
        { id: 'Other', name: 'Diğer cihaz' }
    ]);
    self.selectedCategoryId = ko.observable(null);
    self.productSearchQuery = ko.observable('');
    self.showRecipes = ko.observable(false);
    self.cartItems = ko.observableArray([]);
    self.clientId = ko.observable(null);
    self.selectedPersonnelId = ko.observable(null);
    self.paymentMethodId = ko.observable('1');
    self.giftCardCode = ko.observable('');
    self.discountAmount = ko.observable(0);
    self.tipAmount = ko.observable(0);
    self.tipIncludeInTotal = ko.observable(false); // BUG.A2: bahsis toplama dahil mi
    self.linkedAppointmentId = ko.observable(null);
    self.currentMaterialItem = ko.observable(null);
    self.todayAppointments = ko.observableArray([]);
    self.appointmentsLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.isPrepaid = ko.observable(false);
    self.prepaidAmount = ko.observable(0);
    self.sessionUsagePackage = ko.observable(null);
    self.sessionUseForm = {
        area: ko.observable(''),
        productId: ko.observable(null),
        amount: ko.observable(''),
        deviceType: ko.observable(''),
        deviceModel: ko.observable(''),
        settings: ko.observable(''),
        reaction: ko.observable(''),
        nextDate: ko.observable(''),
        notes: ko.observable('')
    };

    function readError(xhr, fallback) {
        if (typeof xhr.responseJSON === 'string') return xhr.responseJSON;
        return xhr.responseJSON?.error || xhr.responseJSON?.message || xhr.responseText || fallback;
    }

    function normalizeList(data) {
        if (Array.isArray(data)) return data;
        if (data && Array.isArray(data.items)) return data.items;
        return [];
    }

    function toOptionalInt(value) {
        var parsed = parseInt(value, 10);
        return parsed > 0 ? parsed : null;
    }

    self.formatMoney = function (value) {
        return (parseFloat(value) || 0).toLocaleString(document.documentElement.lang || undefined) + ' TL';
    };

    self.ensureCurrentPersonnelOption = function () {
        var currentPersonnelId = toOptionalInt(window.slnCurrentPersonnelId);
        if (!currentPersonnelId || window.slnCurrentRoleId === 101) return null;

        var existing = self.staffList().find(function (p) { return parseInt(p.id, 10) === currentPersonnelId; });
        if (existing) return existing;

        var option = {
            id: currentPersonnelId,
            fullName: window.slnCurrentFullName || slnJsT('salon.sidebar.staff', 'Personel'),
            branchId: window.slnCurrentJwtBranchId || null
        };
        self.staffList.push(option);
        return option;
    };

    self.selectDefaultPersonnel = function () {
        if (self.selectedPersonnelId()) return;

        var current = self.ensureCurrentPersonnelOption();
        if (current) {
            self.selectedPersonnelId(String(current.id));
            return;
        }

        if (self.staffList().length === 1) {
            self.selectedPersonnelId(String(self.staffList()[0].id));
        }
    };

    function readItemUnitPrice(item) {
        var value = parseFloat(typeof item.editPrice === 'function' ? item.editPrice() : item.editPrice);
        return isNaN(value) ? item.unitPrice : value;
    }

    function createMaterialUsage(source) {
        source = source || {};
        var product = self.products().find(function (p) { return p.id === source.productId; });
        return {
            productId: ko.observable(source.productId || null),
            quantity: ko.observable(source.quantity || 1),
            unit: ko.observable(source.unit || product?.unit || 'Adet'),
            notes: ko.observable(source.notes || ''),
            productName: source.productName || product?.name || ''
        };
    }

    function ensureMaterialFields(item) {
        if (!item) return item;
        if (typeof item.materialUsages !== 'function') item.materialUsages = ko.observableArray(item.materialUsages || []);
        if (typeof item.noMaterialUsed !== 'function') item.noMaterialUsed = ko.observable(item.noMaterialUsed || false);
        if (!('materialAutoScaled' in item)) item.materialAutoScaled = true;
        return item;
    }

    self.findRecipeForService = function (serviceId) {
        return self.recipes().find(function (r) {
            return r.isActive !== false && parseInt(r.serviceId, 10) === parseInt(serviceId, 10);
        }) || null;
    };

    self.applyDefaultRecipeMaterials = function (item) {
        ensureMaterialFields(item);
        if (!item || !item.serviceId || item.materialUsages().length > 0) return;

        var recipe = self.findRecipeForService(item.serviceId);
        if (!recipe || !recipe.items || recipe.items.length === 0) return;

        var qty = parseFloat(typeof item.quantity === 'function' ? item.quantity() : item.quantity) || 1;
        item.materialUsages(recipe.items.map(function (recipeItem) {
            return createMaterialUsage({
                productId: recipeItem.productId,
                productName: recipeItem.productName,
                quantity: (parseFloat(recipeItem.quantity) || 0) * qty,
                unit: recipeItem.unit,
                notes: recipe.name + (recipeItem.notes ? ' - ' + recipeItem.notes : '')
            });
        }));
        item.noMaterialUsed(false);
        item.materialAutoScaled = true;
    };

    function scaleMaterialUsages(item, oldQuantity, newQuantity) {
        ensureMaterialFields(item);
        if (!item.materialAutoScaled || item.materialUsages().length === 0) return;
        oldQuantity = parseFloat(oldQuantity) || 1;
        newQuantity = parseFloat(newQuantity) || 1;
        if (oldQuantity <= 0 || newQuantity <= 0) return;

        var ratio = newQuantity / oldQuantity;
        item.materialUsages().forEach(function (usage) {
            var current = parseFloat(usage.quantity()) || 0;
            usage.quantity(Math.round(current * ratio * 1000) / 1000);
        });
    }

    self.materialSummary = function (item) {
        ensureMaterialFields(item);
        var count = item.materialUsages().filter(function (m) {
            return parseInt(m.productId(), 10) > 0 && (parseFloat(m.quantity()) || 0) > 0;
        }).length;
        if (count === 0 && item.noMaterialUsed && item.noMaterialUsed()) return 'Sarf yok';
        return count > 0 ? count + ' sarf' : 'Sarf ekle';
    };

    self.materialButtonClass = function (item) {
        ensureMaterialFields(item);
        return readMaterialConsumptions(item).length > 0 || (item.noMaterialUsed && item.noMaterialUsed())
            ? 'btn-outline-warning'
            : 'btn-warning text-dark';
    };

    self.openMaterials = function (item) {
        ensureMaterialFields(item);
        self.applyDefaultRecipeMaterials(item);
        self.currentMaterialItem(item);
        new bootstrap.Modal(document.getElementById('materialModal')).show();
    };

    self.addMaterialUsage = function () {
        var item = self.currentMaterialItem();
        if (!item) return;
        ensureMaterialFields(item);
        item.materialAutoScaled = false;
        item.noMaterialUsed(false);
        item.materialUsages.push(createMaterialUsage());
    };

    self.removeMaterialUsage = function (usage) {
        var item = self.currentMaterialItem();
        if (!item) return;
        item.materialAutoScaled = false;
        item.materialUsages.remove(usage);
    };

    self.markNoMaterialUsed = function () {
        var item = self.currentMaterialItem();
        if (!item) return;
        ensureMaterialFields(item);
        item.materialUsages([]);
        item.noMaterialUsed(true);
    };

    function findMissingMaterialItem() {
        return self.cartItems().find(function (item) {
            ensureMaterialFields(item);
            return item.serviceId
                && item.forceSessionSale !== true
                && readMaterialConsumptions(item).length === 0
                && !(item.noMaterialUsed && item.noMaterialUsed());
        }) || null;
    }

    function readMaterialConsumptions(item) {
        ensureMaterialFields(item);
        if (!item.serviceId) return [];

        return item.materialUsages().map(function (usage) {
            return {
                productId: parseInt(usage.productId(), 10) || 0,
                quantity: parseFloat(usage.quantity()) || 0,
                unit: usage.unit() || null,
                notes: usage.notes() || null
            };
        }).filter(function (usage) {
            return usage.productId > 0 && usage.quantity > 0;
        });
    }

    // ═══ Autocomplete ═══
    self.clientAutocomplete = createAutocomplete(self.clientList, 'fullName', self.clientId);

    self.ensureBenefitFields = function (item) {
        if (typeof item.benefitText !== 'function') item.benefitText = ko.observable(item.benefitText || null);
        if (!('membershipId' in item)) item.membershipId = null;
        if (!('useMembershipBenefit' in item)) item.useMembershipBenefit = false;
        if (!('clientPackageId' in item)) item.clientPackageId = null;
        if (!('usePackageSession' in item)) item.usePackageSession = false;
        if (!('packageRemainingSessions' in item)) item.packageRemainingSessions = null;
        return item;
    };

    self.resetServiceBenefit = function (item, resetPrice) {
        self.ensureBenefitFields(item);
        if (!item.serviceId) return;
        item.membershipId = null;
        item.useMembershipBenefit = false;
        item.clientPackageId = null;
        item.usePackageSession = false;
        item.packageRemainingSessions = null;
        item.benefitText(null);
        if (resetPrice !== false) item.editPrice(item.unitPrice);
    };

    // Musteri secildiginde uyelik kontrolu
    self.clientId.subscribe(function (newClientId) {
        self.loadClientPackages(newClientId);
        self.loadLoyaltyRewards(newClientId);
        self.loadClientLoyaltyBalance(newClientId);
        self.loadServiceSessionPlans(newClientId);
        self.loyaltyPointsToRedeem(0);
        if (!newClientId || self.cartItems().length === 0) return;
        self.applyClientBenefits();
    });

    self.applyMembershipBenefits = function () {
        self.applyClientBenefits();
    };

    self.applyPackageBenefitsToItems = function (packages, serviceItems) {
        (packages || []).forEach(function (pkg) {
            var remaining = parseInt(pkg.remainingSessions, 10) || 0;
            if (remaining <= 0) return;

            var packageServiceId = parseInt(pkg.serviceId, 10);
            var item = serviceItems.find(function (i) {
                return parseInt(i.serviceId, 10) === packageServiceId && i.usePackageSession !== true;
            });
            if (!item) return;

            item.clientPackageId = pkg.clientPackageId || pkg.id;
            item.usePackageSession = true;
            item.packageRemainingSessions = remaining;
            item.editPrice(0);
            item.benefitText(
                pkg.packageName + ': '
                + slnJsT('salon.sales.session_plan_available_suffix', 'satilmis seans planindan dusulecek')
                + ' (' + slnJsT('salon.packages.auto.kalan', 'kalan') + ' ' + remaining + ')'
            );
        });
    };

    self.applyClientBenefits = function () {
        var clientId = self.clientId();
        if (!clientId) return;

        var serviceItems = self.cartItems().filter(function (i) { return i.serviceId && i.forceSessionSale !== true; });
        serviceItems.forEach(function (item) { self.resetServiceBenefit(item, false); });

        var serviceIds = serviceItems.map(function (i) { return i.serviceId; })
            .filter(function (value, index, arr) { return arr.indexOf(value) === index; });
        if (serviceIds.length === 0) return;

        self.applyPackageBenefitsToItems(self.activeClientPackages ? self.activeClientPackages() : self.clientPackages(), serviceItems);

        $.ajax({
            url: '/proxy/sln-loyalty-packages/usable',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ slnClientId: parseInt(clientId), serviceIds: serviceIds })
        }).done(function (packages) {
            self.applyPackageBenefitsToItems(packages, serviceItems);
        }).always(function () {
            self.applyMembershipOnly();
        });
    };

    self.applyMembershipOnly = function () {
        var clientId = self.clientId();
        if (!clientId) return;
        var serviceIds = self.cartItems().filter(function (i) { return i.serviceId && i.usePackageSession !== true && i.forceSessionSale !== true; }).map(function (i) { return i.serviceId; });
        if (serviceIds.length === 0) return;

        self.cartItems().forEach(function (item) {
                if (!item.serviceId || item.usePackageSession === true || item.forceSessionSale === true) return;
            self.ensureBenefitFields(item);
            item.membershipId = null;
            item.useMembershipBenefit = false;
            item.benefitText(null);
            item.editPrice(item.unitPrice);
            self.setSessionSaleHint(item);
        });

        $.ajax({
            url: '/proxy/sln-memberships/check-benefits',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ slnClientId: parseInt(clientId), serviceIds: serviceIds })
        }).done(function (benefits) {
            if (!benefits || !benefits.length) return;
            self.cartItems().forEach(function (item) {
                if (item.usePackageSession === true) return;
                self.ensureBenefitFields(item);
                var benefit = benefits.find(function (b) { return b.serviceId === item.serviceId; });
                if (!benefit) {
                    self.setSessionSaleHint(item);
                    return;
                }

                if (benefit.hasFreeBenefit && benefit.remainingFree > 0) {
                    // Ucretsiz hakki var
                    item.editPrice(0);
                    item.membershipId = benefit.membershipId;
                    item.useMembershipBenefit = true;
                    item.benefitText(benefit.planName + ': ' + benefit.usedThisPeriod + '/' + benefit.freeCount + ' kullanildi (ucretsiz)');
                } else if (benefit.discountPercent && benefit.discountPercent > 0) {
                    // Indirimli
                    var discounted = item.unitPrice * (1 - benefit.discountPercent / 100);
                    item.editPrice(Math.round(discounted * 100) / 100);
                    item.membershipId = null;
                    item.useMembershipBenefit = false;
                    item.benefitText(benefit.planName + ': ' + slnJsT('salon.sales.membership_discount_suffix', '%{percent} indirim').replace('{percent}', benefit.discountPercent));
                } else {
                    self.setSessionSaleHint(item);
                }
            });
            if (benefits.some(function (b) { return b.hasFreeBenefit || b.discountPercent; }))
                toastr.info(slnJsT('salon.sales.js.uyelik_avantajlari_uygulandi', 'Üyelik avantajları uygulandı.'));
        });
    };

    self.filteredServices = ko.computed(function () {
        var catId = self.selectedCategoryId();
        if (!catId) return self.allServices();
        return self.allServices().filter(function (s) { return s.categoryId === catId && s.isActive; });
    });

    self.filteredSessionDefinitions = ko.computed(function () {
        return self.packageDefinitions().map(function (def) {
            var service = self.allServices().find(function (s) {
                return parseInt(s.id, 10) === parseInt(def.serviceId, 10);
            });
            if (service && service.isActive === false) return null;
            return {
                id: def.id,
                name: def.name,
                serviceId: def.serviceId,
                serviceName: def.serviceName || (service ? service.name : ''),
                categoryId: service ? service.categoryId : null,
                totalSessions: def.totalSessions,
                price: def.price,
                validDays: def.validDays
            };
        }).filter(function (def) {
            return def;
        }).sort(function (a, b) {
            return (a.serviceName || '').localeCompare(b.serviceName || '', document.documentElement.lang || undefined)
                || (a.name || '').localeCompare(b.name || '', document.documentElement.lang || undefined);
        });
    });

    self.filteredProducts = ko.computed(function () {
        var q = (self.productSearchQuery() || '').trim().toLowerCase();
        if (!q) return [];
        return self.products().filter(function (p) {
            return (p.isActive !== false)
                && (((p.name || '').toLowerCase().indexOf(q) >= 0)
                    || ((p.barcode || '').toLowerCase().indexOf(q) >= 0));
        }).slice(0, 8);
    });

    self.activeClientPackages = ko.computed(function () {
        return self.clientPackages().filter(function (pkg) {
            return pkg.isActive && (parseInt(pkg.remainingSessions, 10) || 0) > 0;
        }).sort(function (a, b) {
            return (a.expiresAt || '').localeCompare(b.expiresAt || '')
                || (a.packageName || '').localeCompare(b.packageName || '', document.documentElement.lang || undefined);
        });
    });

    self.sessionUsageNumber = function (pkg) {
        return (parseInt(pkg.usedSessions, 10) || 0) + 1;
    };

    self.sessionUsageSummary = function (pkg) {
        if (!pkg) return '';
        return self.sessionUsageNumber(pkg) + '/' + (parseInt(pkg.totalSessions, 10) || 0)
            + ' seans - kalan ' + (parseInt(pkg.remainingSessions, 10) || 0);
    };

    self.isLaserSessionPackage = function (pkg) {
        var text = ((pkg && ((pkg.serviceName || '') + ' ' + (pkg.packageName || ''))) || '').toLocaleLowerCase('tr-TR');
        return text.indexOf('lazer') >= 0 || text.indexOf('epilasyon') >= 0;
    };

    self.sessionDeviceText = function () {
        var deviceType = self.sessionUseForm.deviceType();
        var device = self.laserDevices().find(function (item) { return item.id === deviceType; });
        var model = (self.sessionUseForm.deviceModel() || '').trim();
        if (!deviceType && !model) return '';
        if (deviceType === 'Other') return model || 'Diğer cihaz';
        return [device ? device.name : deviceType, model].filter(Boolean).join(' - ');
    };

    self.subtotal = ko.computed(function () {
        var total = 0;
        self.cartItems().forEach(function (item) { total += item.quantity() * (parseFloat(item.editPrice()) || 0); });
        return total;
    });

    self.sessionDefinitionForService = function (serviceId) {
        var id = parseInt(serviceId) || 0;
        if (!id) return null;
        return self.packageDefinitions().find(function (def) {
            return def.isActive !== false && parseInt(def.serviceId) === id;
        }) || null;
    };

    self.sessionDefinitionLabel = function (service) {
        var def = self.sessionDefinitionForService(service && service.id);
        if (!def) return '';
        return (parseInt(def.totalSessions) || 0) + ' ' + slnJsT('salon.services.package_sessions', 'seans');
    };

    self.setSessionSaleHint = function (item) {
        if (!item || !item.serviceId || item.usePackageSession === true || item.useMembershipBenefit === true) return;
        var def = self.sessionDefinitionForService(item.serviceId);
        if (!def) return;

        self.ensureBenefitFields(item);
        item.benefitText(
            slnJsT('salon.sales.session_plan_sale_hint', 'Odeme alindiginda musteriye {count} seanslik takip acilir.')
                .replace('{count}', parseInt(def.totalSessions) || 0)
        );
    };

    self.addSessionDefinitionToCart = function (def) {
        if (!def) return;
        self.selectDefaultPersonnel();

        var existing = self.cartItems().find(function (item) {
            return item.forceSessionSale === true && parseInt(item.packageDefinitionId, 10) === parseInt(def.id, 10);
        });
        if (existing) {
            existing.quantity(existing.quantity() + 1);
            return;
        }

        self.cartItems.push({
            serviceId: def.serviceId,
            productId: null,
            packageDefinitionId: def.id,
            loyaltyPackageOfferId: def.id,
            forceSessionSale: true,
            name: def.name || def.serviceName,
            unitPrice: def.price || 0,
            editPrice: ko.observable(def.price || 0),
            quantity: ko.observable(1),
            benefitText: ko.observable(
                slnJsT('salon.sales.loyalty_package_sale_hint', 'Odeme alindiginda musteriye {count} seanslik sadakat paketi acilir.')
                    .replace('{count}', parseInt(def.totalSessions, 10) || 0)
            ),
            materialUsages: ko.observableArray([]),
            noMaterialUsed: ko.observable(true)
        });
    };

    self.grandTotal = ko.computed(function () {
        var tip = self.tipIncludeInTotal() ? (parseFloat(self.tipAmount()) || 0) : 0;
        return Math.max(0, self.subtotal() - (parseFloat(self.discountAmount()) || 0) + tip);
    });

    // ═══ Data Loading ═══
    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-services/categories', method: 'GET' }).done(function (data) {
            var categories = normalizeList(data).filter(function (cat) {
                return cat && cat.isActive !== false;
            });
            self.categories(categories);

            var services = [];
            categories.forEach(function (cat) {
                (cat.services || []).forEach(function (svc) {
                    if (svc.isActive === false) return;
                    svc.categoryId = cat.id;
                    svc.categoryColor = cat.color;
                    services.push(svc);
                });
            });
            self.allServices(services);

            var selectedCategory = categories.find(function (cat) {
                return (cat.services || []).some(function (svc) { return svc.isActive !== false; });
            }) || categories[0];
            self.selectedCategoryId(selectedCategory ? selectedCategory.id : null);

            if (categories.length === 0 || services.length === 0) {
                toastr.warning('Hızlı satışta gösterilecek aktif hizmet bulunamadı. Hizmetler ekranından aktif hizmet ekleyin.');
            }
        }).fail(function (xhr) {
            self.categories([]);
            self.allServices([]);
            toastr.error(readError(xhr, 'Hizmetler yüklenemedi'));
        });
        self.loadProducts();
        self.loadLoyaltyConfig();
        $.ajax({ url: '/proxy/sln-clients?pageSize=1000', method: 'GET' }).done(function (data) {
            self.clientList(data.items || data);
        });
        $.ajax({ url: '/proxy/portal/personnel', method: 'GET' }).done(function (data) {
            self.staffList(data.items || data);
            self.selectDefaultPersonnel();
        }).fail(function () {
            self.ensureCurrentPersonnelOption();
            self.selectDefaultPersonnel();
        });
        $.ajax({ url: '/proxy/sln-recipes', method: 'GET' }).done(function (data) {
            self.recipes((data.items || data).filter(function (r) { return r.isActive; }));
        });
        $.ajax({ url: '/proxy/sln-loyalty-packages/offers', method: 'GET' }).done(function (data) {
            self.packageDefinitions(normalizeList(data).filter(function (d) { return d.isActive !== false; }));
        }).fail(function () {
            self.packageDefinitions([]);
        });
    };

    self.loadServiceSessionPlans = function (clientId) {
        if (!clientId) { self.serviceSessionPlans([]); return; }
        $.ajax({ url: '/proxy/sln-service-sessions/plans?clientId=' + parseInt(clientId, 10) + '&activeOnly=true', method: 'GET' })
            .done(function (data) {
                var list = Array.isArray(data) ? data : (data && Array.isArray(data.items) ? data.items : []);
                self.serviceSessionPlans(list.filter(function (p) {
                    return p && p.isActive !== false && (parseInt(p.remainingSessions, 10) || 0) > 0;
                }));
            })
            .fail(function () { self.serviceSessionPlans([]); });
    };

    self.addPlanSessionToCart = function (plan) {
        if (!plan || !plan.serviceId || (parseInt(plan.remainingSessions, 10) || 0) <= 0) return;
        var already = self.cartItems().some(function (i) { return i.serviceSessionPlanId === plan.id; });
        if (already) { toastr.info(slnJsT('salon.sales.js.plan_already_in_cart', 'Bu plandan kalem zaten sepette')); return; }
        self.cartItems.push({
            serviceId: parseInt(plan.serviceId, 10),
            productId: null,
            name: (plan.serviceName || 'Hizmet') + ' (Plan Seansi #' + ((parseInt(plan.usedSessions, 10) || 0) + 1) + ')',
            quantity: ko.observable(1),
            editPrice: ko.observable(0),
            unitPrice: 0,
            discountAmount: ko.observable(0),
            membershipId: null,
            useMembershipBenefit: false,
            clientPackageId: null,
            usePackageSession: false,
            loyaltyPackageOfferId: null,
            loyaltyRewardId: null,
            serviceSessionPlanId: plan.id,
            isPlanSession: true,
            forceSessionSale: false,
            materialConsumptions: ko.observableArray([])
        });
        toastr.success(slnJsT('salon.sales.js.plan_session_added', 'Plan seansi sepete eklendi'));
    };

    self.loadLoyaltyConfig = function () {
        $.ajax({ url: '/proxy/sln-loyalty/config', method: 'GET' })
            .done(function (data) { self.loyaltyConfig(data || null); })
            .fail(function () { self.loyaltyConfig(null); });
    };

    self.loadLoyaltyRewards = function (clientId) {
        if (!clientId) { self.loyaltyRewards([]); return; }
        $.ajax({ url: '/proxy/sln-loyalty-programs/rewards?clientId=' + parseInt(clientId, 10), method: 'GET' })
            .done(function (data) {
                var list = Array.isArray(data) ? data : (data && Array.isArray(data.items) ? data.items : []);
                self.loyaltyRewards(list);
            })
            .fail(function () { self.loyaltyRewards([]); });
    };

    self.loadClientLoyaltyBalance = function (clientId) {
        if (!clientId) { self.clientLoyaltyBalance(0); return; }
        $.ajax({ url: '/proxy/sln-loyalty/clients?clientId=' + parseInt(clientId, 10), method: 'GET' })
            .done(function (data) {
                var list = Array.isArray(data) ? data : (data && Array.isArray(data.items) ? data.items : []);
                var match = list.find(function (l) { return parseInt(l.slnClientId || l.id) === parseInt(clientId); });
                self.clientLoyaltyBalance(match ? (parseInt(match.currentBalance) || 0) : 0);
            })
            .fail(function () { self.clientLoyaltyBalance(0); });
    };

    self.addRewardToCart = function (reward) {
        if (!reward || !reward.rewardServiceId) return;
        var already = self.cartItems().some(function (i) { return i.loyaltyRewardId === reward.id; });
        if (already) { toastr.info(slnJsT('salon.sales.js.reward_already_in_cart', 'Bu odul zaten sepette')); return; }
        self.cartItems.push({
            serviceId: parseInt(reward.rewardServiceId, 10),
            productId: null,
            name: (reward.rewardServiceName || 'Odul') + ' (Sadakat Odulu)',
            quantity: ko.observable(1),
            editPrice: ko.observable(0),
            unitPrice: 0,
            discountAmount: ko.observable(0),
            membershipId: null,
            useMembershipBenefit: false,
            clientPackageId: null,
            usePackageSession: false,
            loyaltyRewardId: reward.id,
            isLoyaltyReward: true,
            forceSessionSale: false,
            materialConsumptions: ko.observableArray([])
        });
        toastr.success(slnJsT('salon.sales.js.reward_added', 'Odul sepete eklendi'));
    };

    self.loyaltyPointsTlValue = ko.computed(function () {
        var pts = parseInt(self.loyaltyPointsToRedeem(), 10) || 0;
        var cfg = self.loyaltyConfig();
        if (!cfg || pts <= 0) return 0;
        return Math.round(pts * (parseFloat(cfg.pointValue) || 0) * 100) / 100;
    });

    self.loadClientPackages = function (clientId) {
        if (!clientId) {
            self.clientPackages([]);
            return;
        }

        $.ajax({ url: '/proxy/sln-loyalty-packages/purchases?clientId=' + parseInt(clientId, 10), method: 'GET' })
            .done(function (data) {
                self.clientPackages(normalizeList(data));
            })
            .fail(function () {
                self.clientPackages([]);
            });
    };

    self.loadProducts = function () {
        $.ajax({ url: '/proxy/sln-products', method: 'GET' })
            .done(function (data) { self.products(data.items || data); })
            .fail(function () { self.products([]); });
    };

    // ═══ Recipe Toggle ═══
    self.toggleRecipes = function () {
        self.showRecipes(!self.showRecipes());
        if (self.showRecipes()) self.selectedCategoryId(null);
    };

    // ═══ Add Recipe to Cart ═══
    self.addRecipeToCart = function (recipe) {
        self.selectDefaultPersonnel();
        (recipe.items || []).forEach(function (item) {
            for (var i = 0; i < item.quantity; i++) {
                var existing = self.cartItems().find(function (c) { return c.serviceId === item.serviceId; });
                if (existing) {
                    var oldQuantity = existing.quantity();
                    var newQuantity = oldQuantity + 1;
                    existing.quantity(newQuantity);
                    scaleMaterialUsages(existing, oldQuantity, newQuantity);
                } else {
                    var recipeCartItem = {
                        serviceId: item.serviceId,
                        forceSessionSale: false,
                        name: item.serviceName,
                        unitPrice: item.servicePrice,
                        editPrice: ko.observable(item.servicePrice),
                        quantity: ko.observable(1),
                        benefitText: ko.observable(null),
                        materialUsages: ko.observableArray([])
                    };
                    self.applyDefaultRecipeMaterials(recipeCartItem);
                    self.cartItems.push(recipeCartItem);
                }
            }
        });
        toastr.info(recipe.name + ' sepete eklendi');
    };

    // ═══ Category Selection ═══
    self.selectCategory = function (cat) {
        self.selectedCategoryId(cat.id);
    };

    // ═══ Cart Operations ═══
    self.addToCart = function (service) {
        self.selectDefaultPersonnel();
        // Ayni hizmet varsa adet arttir
        var existing = self.cartItems().find(function (item) { return item.serviceId === service.id; });
        if (existing) {
            var oldQuantity = existing.quantity();
            var newQuantity = oldQuantity + 1;
            existing.quantity(newQuantity);
            scaleMaterialUsages(existing, oldQuantity, newQuantity);
            self.applyMembershipBenefits();
            return;
        }
        var cartItem = {
            serviceId: service.id,
            forceSessionSale: false,
            name: service.name,
            unitPrice: service.price,
            editPrice: ko.observable(service.price),
            quantity: ko.observable(1),
            benefitText: ko.observable(null),
            materialUsages: ko.observableArray([])
        };
        self.setSessionSaleHint(cartItem);
        self.applyDefaultRecipeMaterials(cartItem);
        self.cartItems.push(cartItem);
        // Uyelik kontrolu
        self.applyMembershipBenefits();
    };

    self.addProductToCart = function (product) {
        self.selectDefaultPersonnel();
        var stock = parseFloat(product.stockQuantity) || 0;
        if (stock <= 0) {
            toastr.warning(slnJsT('salon.sales.js.urun_stogu_yok', 'Ürün stoğu yok'));
            return;
        }

        var existing = self.cartItems().find(function (item) { return item.productId === product.id; });
        if (existing) {
            var nextQuantity = existing.quantity() + 1;
            if (nextQuantity > stock) {
                toastr.warning(slnJsT('salon.sales.js.insufficient_stock_prefix', 'Yetersiz stok: ') + product.name);
                return;
            }
            existing.quantity(nextQuantity);
            return;
        }

        self.cartItems.push({
            serviceId: null,
            forceSessionSale: false,
            productId: product.id,
            name: product.name,
            unitPrice: product.salePrice || 0,
            editPrice: ko.observable(product.salePrice || 0),
            quantity: ko.observable(1),
            stockQuantity: stock,
            benefitText: ko.observable(null),
            materialUsages: ko.observableArray([])
        });
    };

    self.addProductBySearch = function () {
        var q = (self.productSearchQuery() || '').trim().toLowerCase();
        if (!q) return;

        var exact = self.products().find(function (p) {
            return (p.barcode || '').toLowerCase() === q;
        });
        var matches = self.filteredProducts();
        var product = exact || (matches.length === 1 ? matches[0] : null);

        if (!product) {
            toastr.warning(slnJsT('salon.sales.js.urun_bulunamadi', 'Ürün bulunamadı'));
            return;
        }

        self.addProductToCart(product);
        self.productSearchQuery('');
    };

    self.onProductSearchKeydown = function (_, event) {
        if (event.key === 'Enter') {
            self.addProductBySearch();
            return false;
        }
        return true;
    };

    self.increaseQty = function (item) {
        if (item.productId && item.quantity() + 1 > item.stockQuantity) {
            toastr.warning(slnJsT('salon.sales.js.insufficient_stock_prefix', 'Yetersiz stok: ') + item.name);
            return;
        }
        if (item.usePackageSession === true && item.quantity() + 1 > item.packageRemainingSessions) {
            toastr.warning(slnJsT('salon.sales.js.paket_seansi_yetersiz', 'Paket seansi yetersiz: ') + item.name);
            return;
        }
        var oldQuantity = item.quantity();
        var newQuantity = oldQuantity + 1;
        item.quantity(newQuantity);
        if (item.serviceId && item.forceSessionSale !== true) scaleMaterialUsages(item, oldQuantity, newQuantity);
        if (item.serviceId) self.applyClientBenefits();
    };

    self.decreaseQty = function (item) {
        if (item.quantity() > 1) {
            var oldQuantity = item.quantity();
            var newQuantity = oldQuantity - 1;
            item.quantity(newQuantity);
            if (item.serviceId && item.forceSessionSale !== true) scaleMaterialUsages(item, oldQuantity, newQuantity);
        } else {
            self.cartItems.remove(item);
        }
        if (item.serviceId) self.applyClientBenefits();
    };

    self.removeFromCart = function (item) {
        self.cartItems.remove(item);
        if (item.serviceId) self.applyClientBenefits();
    };

    // ═══ Checkout ═══
    // Asil odeme islemi (personel + musteri kontrolleri gectikten sonra)
    self._executeCheckout = function () {
        var items = self.cartItems().map(function (item) {
            return {
                serviceId: item.serviceId,
                productId: item.productId || null,
                personnelId: self.selectedPersonnelId() ? parseInt(self.selectedPersonnelId()) : null,
                quantity: item.quantity(),
                unitPrice: readItemUnitPrice(item),
                discountAmount: 0,
                membershipId: item.useMembershipBenefit === true ? item.membershipId : null,
                useMembershipBenefit: item.useMembershipBenefit === true,
                loyaltyPackagePurchaseId: item.usePackageSession === true ? item.clientPackageId : null,
                usePackageSession: item.usePackageSession === true,
                loyaltyPackageOfferId: item.loyaltyPackageOfferId || null,
                serviceSessionPlanId: item.serviceSessionPlanId || null,
                loyaltyRewardId: item.loyaltyRewardId || null,
                materialConsumptions: readMaterialConsumptions(item)
            };
        });

        var data = {
            slnClientId: self.clientId() ? parseInt(self.clientId()) : null,
            slnAppointmentId: self.linkedAppointmentId() ? parseInt(self.linkedAppointmentId()) : null,
            paymentMethodId: parseInt(self.paymentMethodId()) || 1,
            giftCardCode: parseInt(self.paymentMethodId()) === 5 ? self.giftCardCode() : null,
            discountAmount: parseFloat(self.discountAmount()) || 0,
            tipAmount: parseFloat(self.tipAmount()) || 0,
            includeTipInTotal: self.tipIncludeInTotal() === true,
            notes: self.isPrepaid() ? slnJsT('salon.sales.note.prepayment_prefix', 'Ön ödeme') + ': ' + self.prepaidAmount() + ' TL (Online)' : null,
            prepaidAmount: self.prepaidAmount(),
            loyaltyPointsToRedeem: parseInt(self.loyaltyPointsToRedeem(), 10) > 0 ? parseInt(self.loyaltyPointsToRedeem(), 10) : null,
            items: items
        };

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-finance/invoices',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            toastr.success(slnJsT('salon.sales.js.odeme_alindi', 'Ödeme alındı'));

            self.cartItems([]);
            self.loadProducts();
            self.clientId(null);
            self.clientAutocomplete.clear();
            self.clientPackages([]);
            self.loyaltyRewards([]);
            self.loyaltyPointsToRedeem(0);
            self.clientLoyaltyBalance(0);
            self.serviceSessionPlans([]);
            self.discountAmount(0);
            self.tipAmount(0);
            self.giftCardCode('');
            self.linkedAppointmentId(null);
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(readError(xhr, slnJsT('salon.sales.js.odeme_alinamadi', 'Ödeme alınamadı')));
            self.isSaving(false);
        });
    };

    self.checkout = function () {
        if (self.cartItems().length === 0) {
            toastr.warning(slnJsT('salon.sales.js.cart_empty', 'Sepet boş'));
            return;
        }

        if (parseInt(self.paymentMethodId()) === 5 && !(self.giftCardCode() || '').trim()) {
            toastr.warning(slnJsT('salon.sales.js.gift_card_code_required', 'Hediye kartı kodu girilmelidir'));
            return;
        }

        var missingMaterialItem = findMissingMaterialItem();
        if (missingMaterialItem) {
            self.openMaterials(missingMaterialItem);
            toastr.warning('Bu hizmette ne kullanildigini yazin ya da "Malzeme yok" secin.');
            return;
        }

        // BUG2.2/PAY.4: Musteri secilmediyse ad/soyad sor, hizli musteri olustur
        if (!self.clientId()) {
            confirmModal(slnJsT('salon.sales.js.hizli_musteri', 'Hızlı Müşteri'), slnJsT('salon.sales.js.musteri_secilmedi_tahsilat_icin_ad_soyad_girin', 'Müşteri seçilmedi. Tahsilat için ad-soyad girin:'), function (name) {
                name = (name || '').trim();
                if (!name) { toastr.warning(slnJsT('salon.sales.js.musteri_adi_gerekli', 'Müşteri adı gerekli.')); return; }

                var body = { fullName: name };
                self.isSaving(true);
                $.ajax({
                    url: '/proxy/sln-clients',
                    method: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    dataType: 'json',
                    data: JSON.stringify(body)
                }).done(function (resp) {
                    var newId = (resp && (resp.id || resp.Id)) || null;
                    if (!newId) {
                        console.error('[hizli musteri] gecerli id yok', resp);
                        toastr.error(slnJsT('salon.sales.js.musteri_olusturulamadi_kimlik_donmedi', 'Müşteri oluşturulamadı (kimlik dönmedi).'));
                        self.isSaving(false);
                        return;
                    }
                    self.clientId(newId);
                    // Autocomplete gorunumunu de senkronize et
                    if (self.clientAutocomplete) {
                        self.clientAutocomplete.query(name);
                        if (typeof self.clientAutocomplete.selectedName === 'function') {
                            self.clientAutocomplete.selectedName(name);
                        }
                    }
                    self.isSaving(false);
                    self.checkout(); // recurse — personel kontrolu icin
                }).fail(function (xhr) {
                    console.error('[hizli musteri] POST failed', xhr.status, xhr.responseText);
                    var msg = (xhr.responseJSON && (xhr.responseJSON.message || xhr.responseJSON.error))
                        || (slnJsT('salon.sales.customer_create_failed_http', 'Müşteri oluşturulamadı') + ' (HTTP ' + xhr.status + ').');
                    toastr.error(msg);
                    self.isSaving(false);
                });
            }, { input: true, inputLabel: slnJsT('salon.appointments.full_name_required', 'Ad Soyad *'), confirmText: slnJsT('salon.common.continue', 'Devam'), confirmClass: 'btn-primary' });
            return;
        }

        // BUG2.1: Personel secilmediyse uyari
        if (!self.selectedPersonnelId()) {
            confirmModal(
                slnJsT('salon.sales.staff_not_selected', 'Personel Seçilmedi'),
                slnJsT('salon.sales.staff_not_selected_confirm', 'Bu tahsilat personele atanmadan kaydedilecek. Devam edilsin mi?'),
                function () { self._executeCheckout(); },
                { confirmText: slnJsT('salon.common.continue_action', 'Devam Et'), confirmClass: 'btn-warning' }
            );
            return;
        }

        self._executeCheckout();
    };

    // ═══ Randevu Çek ═══
    var appointmentModal;

    self.openAppointments = function () {
        self.appointmentsLoading(true);
        self.todayAppointments([]);
        if (!appointmentModal) appointmentModal = new bootstrap.Modal(document.getElementById('appointmentModal'));
        appointmentModal.show();

        var today = new Date();
        var todayStr = today.getFullYear() + '-' + String(today.getMonth() + 1).padStart(2, '0') + '-' + String(today.getDate()).padStart(2, '0');
        var tomorrowDate = new Date(today); tomorrowDate.setDate(tomorrowDate.getDate() + 1);
        var tomorrowStr = tomorrowDate.getFullYear() + '-' + String(tomorrowDate.getMonth() + 1).padStart(2, '0') + '-' + String(tomorrowDate.getDate()).padStart(2, '0');

        $.get('/proxy/sln-appointments?from=' + todayStr + '&to=' + tomorrowStr, function (data) {
            var list = (data.items || data || []).filter(function (a) {
                // Sadece planlanan(1) ve onaylanan(2) randevulari goster
                return a.statusId === 1 || a.statusId === 2;
            }).map(function (a) {
                // BUG2.17: Naive saat — DB Utc kind ile yazar ama saat LOCAL temsilidir
                a.startTimeText = a.startTime ? a.startTime.substring(11, 16) : '';
                a.clientName = a.clientName || '-';
                a.personnelName = a.personnelName || null;
                a.serviceNamesText = (a.serviceNames || []).join(', ') || (a.serviceName || '-');
                return a;
            });
            self.todayAppointments(list);
            self.appointmentsLoading(false);
        }).fail(function () { self.appointmentsLoading(false); });
    };

    self.remainingAmount = ko.computed(function () {
        return Math.max(0, self.grandTotal() - self.prepaidAmount());
    });

    self.selectAppointment = function (appt) {
        // Sepeti temizle
        self.cartItems([]);

        // Ön ödeme kontrolü
        self.isPrepaid(appt.isPrepaid || false);
        self.prepaidAmount(appt.prepaidAmount || 0);

        // Müşteriyi seç
        if (appt.slnClientId) {
            self.clientId(appt.slnClientId);
            self.clientAutocomplete.query(appt.clientName || '');
            self.clientAutocomplete.selectedName(appt.clientName || '');
            self.loadClientPackages(appt.slnClientId);
        }

        // Personeli seç
        if (appt.personnelId) {
            self.selectedPersonnelId(appt.personnelId.toString());
        }

        // Hizmetleri sepete ekle
        var services = appt.services || [];
        if (services.length > 0) {
            services.forEach(function (s) {
                var svc = self.allServices().find(function (sv) { return sv.id === (s.slnServiceId || s.serviceId); });
                if (svc) {
                    var apptItem = {
                        serviceId: svc.id,
                        forceSessionSale: false,
                        name: svc.name,
                        unitPrice: svc.price,
                        editPrice: ko.observable(svc.price),
                        quantity: ko.observable(1),
                        benefitText: ko.observable(null),
                        materialUsages: ko.observableArray([])
                    };
                    self.applyDefaultRecipeMaterials(apptItem);
                    self.cartItems.push(apptItem);
                }
            });
        } else if (appt.serviceNames && appt.serviceNames.length > 0) {
            appt.serviceNames.forEach(function (svcName) {
                var svc = self.allServices().find(function (sv) { return sv.name === svcName; });
                if (svc) {
                    var namedApptItem = {
                        serviceId: svc.id,
                        forceSessionSale: false,
                        name: svc.name,
                        unitPrice: svc.price,
                        editPrice: ko.observable(svc.price),
                        quantity: ko.observable(1),
                        benefitText: ko.observable(null),
                        materialUsages: ko.observableArray([])
                    };
                    self.applyDefaultRecipeMaterials(namedApptItem);
                    self.cartItems.push(namedApptItem);
                }
            });
        }

        // Randevu bağla
        self.linkedAppointmentId(appt.id);
        appointmentModal.hide();

        if (!(appt.isPrepaid && appt.prepaidAmount > 0)) {
            if (appt.slnClientId) {
                self.applyClientBenefits();
            }
            toastr.info(slnJsT('salon.sales.js.randevu_sepete_alindi_ek_hizmet_urun_ekleyebilirsiniz', 'Randevu sepete alindi. Ek hizmet/ürün ekleyebilirsiniz.'));
            return;
        }

        // Ön ödemeli ise direkt tamamla mı sor
        if (appt.isPrepaid && appt.prepaidAmount > 0) {
            confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.sales.js.bu_randevu_online_odenmis', 'Bu randevu online ödenmiş (') + appt.prepaidAmount + slnJsT('salon.sales.js.tl_ek_islem_yoksa_direkt_tamamlansin_mi', ' TL). Ek işlem yoksa direkt tamamlansın mı?'), function() {
                self.completeWithoutPayment();
            });
            return;
        }

        // Üyelik avantajı kontrolü
        if (appt.slnClientId) {
            var serviceIds = self.cartItems().filter(function (i) { return i.serviceId && i.forceSessionSale !== true; }).map(function (i) { return i.serviceId; });
            if (serviceIds.length > 0) {
                $.ajax({
                    url: '/proxy/sln-memberships/check-benefits',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ slnClientId: parseInt(appt.slnClientId), serviceIds: serviceIds })
                }).done(function (benefits) {
                    if (!benefits || !benefits.length) return;

                    var allFree = true;
                    self.cartItems().forEach(function (item) {
                        var b = benefits.find(function (x) { return x.serviceId === item.serviceId; });
                        if (!b) { allFree = false; return; }

                        if (b.hasFreeBenefit && b.remainingFree > 0) {
                            item.editPrice(0);
                            item.membershipId = b.membershipId;
                            item.useMembershipBenefit = true;
                            self.ensureBenefitFields(item);
                            item.benefitText(b.planName + ': ' + slnJsT('salon.sales.membership_free_usage_suffix', 'ücretsiz ({used}/{total})').replace('{used}', b.usedThisPeriod).replace('{total}', b.freeCount));
                        } else if (b.discountPercent && b.discountPercent > 0) {
                            item.editPrice(Math.round(item.unitPrice * (1 - b.discountPercent / 100) * 100) / 100);
                            item.membershipId = null;
                            item.useMembershipBenefit = false;
                            self.ensureBenefitFields(item);
                            item.benefitText(b.planName + ': ' + slnJsT('salon.sales.membership_discount_suffix', '%{percent} indirim').replace('{percent}', b.discountPercent));
                            allFree = false;
                        } else {
                            allFree = false;
                        }
                    });

                    if (allFree && self.cartItems().length > 0) {
                        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.sales.js.tum_hizmetler_uyelik_kapsaminda_ucretsiz_ek_islem_yoksa_direkt_tamamla', 'Tüm hizmetler üyelik kapsamında ücretsiz. Ek işlem yoksa direkt tamamlansın mı?'), function() {
                            self.completeWithoutPayment();
                        });
                        return;
                    }

                    toastr.info(slnJsT('salon.sales.js.uyelik_avantajlari_uygulandi_ek_hizmet_urun_ekleyebilirsiniz', 'Üyelik avantajları uygulandı. Ek hizmet/ürün ekleyebilirsiniz.'));
                });
                return;
            }
        }

        toastr.info(slnJsT('salon.sales.js.randevu_sepete_alindi_ek_hizmet_urun_ekleyebilirsiniz', 'Randevu sepete alındı. Ek hizmet/ürün ekleyebilirsiniz.'));
    };

    self.completeWithoutPayment = function () {
        // Adisyon 0 TL olustur (kayit icin) + randevu tamamla
        var items = self.cartItems().map(function (item) {
            return {
                serviceId: item.serviceId, productId: null,
                personnelId: self.selectedPersonnelId() ? parseInt(self.selectedPersonnelId()) : null,
                quantity: item.quantity(), unitPrice: 0, discountAmount: 0,
                membershipId: item.useMembershipBenefit === true ? item.membershipId : null,
                useMembershipBenefit: item.useMembershipBenefit === true,
                loyaltyPackagePurchaseId: item.usePackageSession === true ? item.clientPackageId : null,
                usePackageSession: item.usePackageSession === true,
                loyaltyPackageOfferId: item.loyaltyPackageOfferId || null,
                serviceSessionPlanId: item.serviceSessionPlanId || null,
                loyaltyRewardId: item.loyaltyRewardId || null,
                materialConsumptions: readMaterialConsumptions(item)
            };
        });

        var data = {
            slnClientId: self.clientId() ? parseInt(self.clientId()) : null,
            slnAppointmentId: self.linkedAppointmentId() ? parseInt(self.linkedAppointmentId()) : null,
            paymentMethodId: 1,
            discountAmount: 0, tipAmount: 0,
            notes: self.isPrepaid() ? slnJsT('salon.sales.note.completed_with_prepayment', 'Ön ödeme ile tamamlandı') : slnJsT('salon.sales.note.completed_with_membership', 'Üyelik kapsamında tamamlandı'),
            prepaidAmount: self.prepaidAmount(),
            loyaltyPointsToRedeem: parseInt(self.loyaltyPointsToRedeem(), 10) > 0 ? parseInt(self.loyaltyPointsToRedeem(), 10) : null,
            items: items
        };

        $.ajax({
            url: '/proxy/sln-finance/invoices', method: 'POST',
            contentType: 'application/json', data: JSON.stringify(data)
        }).done(function () {
            toastr.success(slnJsT('salon.sales.js.islem_tamamlandi_odeme_alinmadi', 'İşlem tamamlandı (ödeme alınmadı).'));
            self.cartItems([]);
            self.clientId(null);
            self.clientAutocomplete.clear();
            self.clientPackages([]);
            self.loyaltyRewards([]);
            self.loyaltyPointsToRedeem(0);
            self.clientLoyaltyBalance(0);
            self.serviceSessionPlans([]);
            self.linkedAppointmentId(null);
            self.isPrepaid(false);
            self.prepaidAmount(0);
        }).fail(function (xhr) { toastr.error(readError(xhr, 'Islem kaydedilemedi.')); });
    };

    self.unlinkAppointment = function () {
        self.linkedAppointmentId(null);
        self.isPrepaid(false);
        self.prepaidAmount(0);
    };

    // ═══ Init ═══
    var sessionUsageModal;

    function selectedProductName(productId) {
        var id = parseInt(productId, 10) || 0;
        var product = self.products().find(function (p) { return parseInt(p.id, 10) === id; });
        return product ? product.name : '';
    }

    self.openSessionUsage = function (pkg) {
        if (!pkg || !pkg.isActive || (parseInt(pkg.remainingSessions, 10) || 0) <= 0) return;
        self.sessionUsagePackage(pkg);
        self.sessionUseForm.area('');
        self.sessionUseForm.productId(null);
        self.sessionUseForm.amount('');
        self.sessionUseForm.deviceType('');
        self.sessionUseForm.deviceModel('');
        self.sessionUseForm.settings('');
        self.sessionUseForm.reaction('');
        self.sessionUseForm.nextDate('');
        self.sessionUseForm.notes('');
        if (!sessionUsageModal) sessionUsageModal = new bootstrap.Modal(document.getElementById('sessionUsageModal'));
        sessionUsageModal.show();
    };

    self.confirmSessionUsage = function () {
        var pkg = self.sessionUsagePackage();
        if (!pkg) return;
        if (self.isLaserSessionPackage(pkg) && !self.sessionUseForm.deviceType()) {
            toastr.warning('Lazer/epilasyon seansı için cihaz seçmelisiniz');
            return;
        }

        var productName = selectedProductName(self.sessionUseForm.productId());
        var deviceText = self.sessionDeviceText();
        var notes = [
            'Seans: ' + self.sessionUsageSummary(pkg),
            self.sessionUseForm.area() ? 'Bölge/işlem: ' + self.sessionUseForm.area() : null,
            productName ? 'Kullanılan ürün: ' + productName : null,
            self.sessionUseForm.amount() ? 'Miktar: ' + self.sessionUseForm.amount() : null,
            deviceText ? 'Cihaz: ' + deviceText : null,
            self.sessionUseForm.settings() ? 'Ayarlar: ' + self.sessionUseForm.settings() : null,
            self.sessionUseForm.reaction() ? 'Cilt/reaksiyon: ' + self.sessionUseForm.reaction() : null,
            self.sessionUseForm.nextDate() ? 'Sonraki seans: ' + self.sessionUseForm.nextDate() : null,
            self.sessionUseForm.notes() ? 'Not: ' + self.sessionUseForm.notes() : null
        ].filter(Boolean).join('\n');

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-loyalty-packages/redeem',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                purchaseId: pkg.id,
                notes: notes || 'Seans kullanımı'
            })
        }).done(function () {
            if (sessionUsageModal) sessionUsageModal.hide();
            toastr.success('Seans kullanımı kaydedildi');
            self.loadClientPackages(self.clientId());
            self.sessionUsagePackage(null);
        }).fail(function (xhr) {
            toastr.error(readError(xhr, 'Seans kullanımı kaydedilemedi'));
        }).always(function () {
            self.isSaving(false);
        });
    };

    $(document).ready(function () {
        self.loadData();
    });
}

ko.applyBindings(new SalesViewModel(), document.getElementById('sales-vm'));
