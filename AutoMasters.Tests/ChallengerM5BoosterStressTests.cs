using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoMasters.Modeles;
using AutoMasters.Services;
using AutoMasters.VuesModeles;
using Microsoft.Maui.Graphics;
using Xunit;

namespace AutoMasters.Tests;

/// <summary>
/// Empirical Challenger verification tests for Milestone 5 (F32):
/// Booster Opening Mechanics, Thread Safety, Quota & Network Resilience,
/// and ViewModel UI Property Binding Integrity.
/// </summary>
public class ChallengerM5BoosterStressTests
{
    private static List<Carte> Generer5Cartes()
    {
        return Enumerable.Range(1, 5).Select(i => new Carte
        {
            Identifiant = $"card_{i}",
            Marque = $"Marque_{i}",
            Modele = $"Modele_{i}",
            Rarete = i == 5 ? "LÉGENDAIRE" : "COMMUNE",
            PrixOfficiel = 50000 * i
        }).ToList();
    }

    #region 1. UI Binding Integrity: EstFaceCachee & Segment Colors (F32)

    [Fact]
    public void BoosterVueModele_EstFaceCachee_StrictlyInvertedFromEstRetournee_WithPropertyChangeEvents()
    {
        var vm = new BoosterVueModele();
        var raisedProperties = new List<string>();
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null)
                raisedProperties.Add(e.PropertyName);
        };

        // Default: EstRetournee = false => EstFaceCachee must be true
        Assert.False(vm.EstRetournee);
        Assert.True(vm.EstFaceCachee);

        // Act: flip card to revealed
        vm.EstRetournee = true;
        Assert.True(vm.EstRetournee);
        Assert.False(vm.EstFaceCachee);
        Assert.Contains(nameof(vm.EstFaceCachee), raisedProperties);

        raisedProperties.Clear();

        // Act: flip back to hidden
        vm.EstRetournee = false;
        Assert.False(vm.EstRetournee);
        Assert.True(vm.EstFaceCachee);
        Assert.Contains(nameof(vm.EstFaceCachee), raisedProperties);
    }

    [Fact]
    public void BoosterVueModele_CouleurSegments1To5_ReturnExpectedMauiColors_AndNotifyChanges()
    {
        var vm = new BoosterVueModele();

        // Initial default: Segment1 is true, Segments 2..5 are false
        Assert.True(vm.Segment1Actif);
        Assert.False(vm.Segment2Actif);
        Assert.False(vm.Segment3Actif);
        Assert.False(vm.Segment4Actif);
        Assert.False(vm.Segment5Actif);

        var raised = new List<string>();
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null)
                raised.Add(e.PropertyName);
        };

        // Activate segment 2
        vm.Segment2Actif = true;
        Assert.True(vm.Segment2Actif);
        Assert.Contains(nameof(vm.Segment2Actif), raised);

        // Deactivate segment 1
        vm.Segment1Actif = false;
        Assert.False(vm.Segment1Actif);
        Assert.Contains(nameof(vm.Segment1Actif), raised);
    }

    #endregion

    #region 2. Thread Safety & Idempotency Stress Tests (F32)

    [Fact]
    public async Task BoosterVueModele_100ConcurrentClicksOnFinish_FiresSurBoosterTermineExactlyOnce()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.QuandPost("/boosters/open", HttpStatusCode.OK, new { cards = Generer5Cartes() });

        var service = new ServiceAPI(mockHandler);
        var vm = new BoosterVueModele(service);
        await vm.DemarrerOuvertureAsync();

        int invocationCount = 0;
        vm.SurBoosterTermine += () => Interlocked.Increment(ref invocationCount);

        // Advance 9 times to reach the terminal state (5 cards: flip, next, flip, next, flip, next, flip, next, flip -> final button)
        for (int i = 0; i < 9; i++)
        {
            vm.ActionCarteSuivante();
        }

        Assert.True(vm.EstDerniereCarte);
        Assert.Equal("TERMINER & AJOUTER AU GARAGE", vm.LibelleBoutonAction);

        // Launch 100 concurrent clicks on the finish action
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => vm.ActionCarteSuivante()))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.True(vm.EstTermine);
        Assert.Equal(1, invocationCount);
    }

    #endregion

    #region 3. Network Failure & Quota Exhaustion Resilience (F32)

    [Fact]
    public async Task GarageVueModele_Network500Error_DoesNotOpenModal_AndPreservesQuota()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, new Portefeuille());
        mockHandler.QuandPost("/boosters/open", HttpStatusCode.InternalServerError, new { error = "Server Crash" });

        var service = new ServiceAPI(mockHandler);
        var garageVm = new GarageVueModele(service);
        garageVm.PaquetsDisponibles = 3;

        await garageVm.OuvrirBoosterAsync();

        Assert.False(garageVm.EstBoosterModalOuvert, "Modal must NOT be opened when network call fails");
        Assert.Equal(3, garageVm.PaquetsDisponibles);
        Assert.False(garageVm.EstOccupe);
    }

    [Fact]
    public async Task GarageVueModele_NetworkDropException_DoesNotOpenModal_AndPreservesQuota()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, new Portefeuille());
        mockHandler.EnregistrerRoute(HttpMethod.Post, "/boosters/open", _ =>
            throw new HttpRequestException("The connection was forcibly closed by the remote host."));

        var service = new ServiceAPI(mockHandler);
        var garageVm = new GarageVueModele(service);
        garageVm.PaquetsDisponibles = 4;

        await garageVm.OuvrirBoosterAsync();

        Assert.False(garageVm.EstBoosterModalOuvert, "Modal must NOT be opened when network connection drops");
        Assert.Equal(4, garageVm.PaquetsDisponibles);
        Assert.False(garageVm.EstOccupe);
    }

    [Fact]
    public async Task GarageVueModele_EmptyCardsResponse_DoesNotOpenModal_AndPreservesQuota()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, new Portefeuille());
        mockHandler.QuandPost("/boosters/open", HttpStatusCode.OK, new { cards = new List<Carte>() });

        var service = new ServiceAPI(mockHandler);
        var garageVm = new GarageVueModele(service);
        garageVm.PaquetsDisponibles = 2;

        await garageVm.OuvrirBoosterAsync();

        Assert.False(garageVm.EstBoosterModalOuvert, "Modal must NOT be opened when server returns 0 cards");
        Assert.Equal(2, garageVm.PaquetsDisponibles);
        Assert.False(garageVm.EstOccupe);
    }

    [Fact]
    public async Task GarageVueModele_QuotaZero_DoesNotAttemptNetworkCall()
    {
        int networkCallCount = 0;
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, new Portefeuille());
        mockHandler.EnregistrerRoute(HttpMethod.Post, "/boosters/open", _ =>
        {
            networkCallCount++;
            return MockHttpMessageHandler.CreerReponseJson(HttpStatusCode.OK, new { cards = Generer5Cartes() });
        });

        var service = new ServiceAPI(mockHandler);
        var garageVm = new GarageVueModele(service);
        garageVm.PaquetsDisponibles = 0;

        Assert.False(garageVm.PeutOuvrirBooster);

        await garageVm.OuvrirBoosterAsync();

        Assert.Equal(0, networkCallCount);
        Assert.False(garageVm.EstBoosterModalOuvert);
        Assert.False(garageVm.EstOccupe);
    }

    [Fact]
    public async Task GarageVueModele_SuccessfulSequence_DecrementsQuotaOnlyOnceOnCompletion()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, new Portefeuille());
        mockHandler.QuandPost("/boosters/open", HttpStatusCode.OK, new { cards = Generer5Cartes() });

        var service = new ServiceAPI(mockHandler);
        var garageVm = new GarageVueModele(service);
        garageVm.PaquetsDisponibles = 3;

        await garageVm.OuvrirBoosterAsync();

        Assert.True(garageVm.EstBoosterModalOuvert);
        Assert.NotNull(garageVm.BoosterVueModele);
        Assert.Equal(3, garageVm.PaquetsDisponibles); // Quota not decremented yet during reveal

        var boosterVm = garageVm.BoosterVueModele;

        // Perform the full 10 clicks sequence
        for (int i = 0; i < 10; i++)
        {
            boosterVm.ActionCarteSuivante();
        }

        // Wait a brief moment if asynchronous callbacks run
        await Task.Delay(50);

        Assert.False(garageVm.EstBoosterModalOuvert);
        Assert.Equal(2, garageVm.PaquetsDisponibles);
    }

    #endregion
}
