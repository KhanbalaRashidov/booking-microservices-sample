﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildingBlocks.Contracts.Grpc;
using FluentAssertions;
using Grpc.Net.Client;
using Integration.Test.Fakes;
using MagicOnion;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace Integration.Test.Booking.Features;

[Collection(nameof(IntegrationTestFixture))]
public class CreateBookingTests
{
    private readonly GrpcChannel _channel;
    private readonly IntegrationTestFixture _fixture;
    private readonly ITestHarness _testHarness;

    public CreateBookingTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;

        _fixture.RegisterTestServices(services =>
        {
            MockFlightGrpcServices(services);
            MockPassengerGrpcServices(services);
        });

        _testHarness = fixture.TestHarness;
        _channel = fixture.Channel;
    }

    // todo: add support test for event-store
    [Fact]
    public async Task should_create_booking_to_event_store_currectly()
    {
        // Arrange
        var command = new FakeCreateBookingCommand().Generate();

        // Act
        var response = await _fixture.SendAsync(command);

        // Assert
        response.Should().BeGreaterOrEqualTo(0);
    }

    private void MockPassengerGrpcServices(IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Singleton(x =>
        {
            var mock = Substitute.For<IPassengerGrpcService>();
            mock.GetById(Arg.Any<long>())
                .Returns(new UnaryResult<PassengerResponseDto>(new FakePassengerResponseDto().Generate()));

            return mock;
        }));
    }

    private void MockFlightGrpcServices(IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Singleton(x =>
        {
            var mock = Substitute.For<IFlightGrpcService>();

            mock.GetById(Arg.Any<long>())
                .Returns(new UnaryResult<FlightResponseDto>(Task.FromResult(new FakeFlightResponseDto().Generate())));

            mock.GetAvailableSeats(Arg.Any<long>())
                .Returns(
                    new UnaryResult<IEnumerable<SeatResponseDto>>(Task.FromResult(FakeSeatsResponseDto.Generate())));

            mock.ReserveSeat(new FakeReserveSeatRequestDto().Generate())
                .Returns(new UnaryResult<SeatResponseDto>(Task.FromResult(FakeSeatsResponseDto.Generate().First())));

            return mock;
        }));
    }
}
