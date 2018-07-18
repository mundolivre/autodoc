#ifndef __UTILS
#define __UTILS

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#ifndef _WINSOCK_DEPRECATED_NO_WARNINGS
#define _WINSOCK_DEPRECATED_NO_WARNINGS
#endif

#ifdef NETDISCOVERY_EXPORTS
#define NETDISCOVERY_API __declspec(dllexport)
#else
#define NETDISCOVERY_API __declspec(dllimport)
#endif

#pragma once
// Win API
#include <windows.h>
#include <winsock2.h>
#include <ws2ipdef.h>
#include <iphlpapi.h>
#include <windns.h> // For ReverseDnsQuery() Function
#include <stdio.h>
#include <stdlib.h>
// C++ API
#include <vector>
#include <map>
#include <string>
#include <sstream>
#include <iomanip>
#include <memory>
// My API
#include "BpxException.h"
#include "NetworkAdapters.h"
#include "NetNeighbor.h"

#pragma comment(lib, "Dnsapi.lib")
#pragma comment(lib, "iphlpapi.lib")
#pragma comment(lib, "ws2_32.lib")

/*
* @brief Callback para ARP Scan
*/
typedef void(*CALLBACK_ARPSCAN) (char * IPv4, char * PhysicalAddress);

/*
* @brief Callback para Descoberta de adaptadores de rede
*/
typedef void(*CALLBACK_ADAPTERS) (
	DWORD Index,
	char *AdapterName,
	char *Description,
	char *MacAddress,
	char *IpAddress,
	char *IpMask,
	char *Gateway);

/*
* @brief Callback para expor log de erros
*/
typedef void(*CALLBACK_ERROR_MESSAGES) (LPCSTR errorMsg);

/*
* @brief Callback para expor os computadores identificados com sistema operacional Windows
*/
typedef void(*CALLBACK_WINDOWS_COMPUTERS) (LPCSTR IPv4, LPCSTR MacAddress, LPCSTR NetBiosName, LPCSTR FQDN);

/*
* @brief Obtém a lista de adaptadores no computador
*/
void GetAdapters(CALLBACK_ADAPTERS fnCallbackAdapter);

/*
* @brief Obtém a lista atualizada de computadores próximos
*/
void GetNetNeighbor(std::map<std::string, Neighbor> &Reachable);

/*
* @brief Obtém o prefixo da rede
*/
char * GetNetworkPrefix(const std::string &IpMask);

/*
* @brief Formata um endereço MAC
*/
std::string FormatMacAddress(unsigned long length, unsigned char PhysicalAddress[]);

/*
* @brief Formata up IP Reverso
*/
void ReverseIP(std::wstring &host);

/*
* @brief Executa consulta DNS de resolução reversa
*/
void ReverseDnsQuery(std::wstring &host);

#endif